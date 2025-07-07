using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging; // 如果是使用 WPF 相关的 BitmapImage 等
using MvCamCtrl.NET;
using DirectionDetection.Enums;
using System.Windows.Media;
using NLog;
using System.IO.Packaging;
namespace DirectionDetection.ViewModels
{
    public  class ParameterSettingViewModel:INotifyPropertyChanged
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(PropertyName));
        }

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //相机相关变量定义
        // ch:判断用户自定义像素格式 | en:Determine custom pixel format
        public const Int32 CUSTOMER_PIXEL_FORMAT = unchecked((Int32)0x80000000);

        MyCamera.MV_CC_DEVICE_INFO_LIST m_stDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        private MyCamera m_MyCamera = new MyCamera();
        
        Thread m_hReceiveThread = null;
        MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

        // ch:用于从驱动获取图像的缓存 | en:Buffer for getting image from driver
        UInt32 m_nBufSizeForDriver = 0;
        IntPtr m_BufForDriver = IntPtr.Zero;
        private static Object BufForDriverLock = new Object();

        public WriteableBitmap m_bitmap = null;
        
     
        int ImageWidth = 0;
        int ImageHeight = 0;
    

       
        //默认以连续采图打开
        private CaptureMode _captureImageMode = CaptureMode.Continuous;
        public CaptureMode CaptureImageMode
        {
            get { return _captureImageMode; }
            set { 
                _captureImageMode = value;
                SetCaptureMode(value);
                OnPropertyChanged();
            }
        }

        //默认是软触发模式
        private TriggerMode _triggermode = TriggerMode.SoftTware;
        public TriggerMode Triggermode
        {
            get { return _triggermode; }
            set
            {
                _triggermode = value;
                SetTriggerMode(value);
                OnPropertyChanged();
            }
        }
     



        private bool m_bGrabbing = false;

        public bool bGrabbing
        {
            get { return m_bGrabbing; }
            set 
            { 
                m_bGrabbing = value;
            }
        }

     

        private ObservableCollection<string> _cbDeviceList;
        private string _selectedItem;

        public ObservableCollection<string> cbDeviceList
        {
            get { return _cbDeviceList; }
            set
            {
                if (_cbDeviceList != value)
                {
                    _cbDeviceList = value;
                    OnPropertyChanged(nameof(_cbDeviceList));
                }
            }
        }

        public string SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }
        }
        //
        private string _selectedOption;

        public string SelectedOption
        {
            get { return _selectedOption; }
            set
            {
                if (_selectedOption != value)
                {
                    _selectedOption = value;
                    OnPropertyChanged();
                }
            }
        }


        public ParameterSettingViewModel()
        {
            QueryDeviceCommand = new RelayCommand(QueryDeviceCommandExecute);
            CapturingImageCommand = new RelayCommand(CapturingImageCommandExecute);
            // 初始化数据
            cbDeviceList = new ObservableCollection<string> { "Item 1", "Item 2", "Item 3" };
            
            try 
            { 
                MyCamera.MV_CC_Initialize_NET();
            } 
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);    
            }
        }
        public RelayCommand CapturingImageCommand;
        public RelayCommand QueryDeviceCommand;
        public RelayCommand StopCapturingCommand;
       
        private void QueryDeviceCommandExecute(object parameter)
        {
            // ch:创建设备列表 | en:Create Device List
            System.GC.Collect();
            cbDeviceList.Clear();
            m_stDeviceList.nDeviceNum = 0;
           
           MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE |MyCamera.MV_GENTL_GIGE_DEVICE, ref m_stDeviceList);

            // ch:在窗体列表中显示设备名 | en:Display device name in the form list
            for (int i = 0; i < m_stDeviceList.nDeviceNum; i++)
            {
                if (m_stDeviceList.pDeviceInfo[i] != IntPtr.Zero)
                {
                    MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                    string strUserDefinedName = "";
                    if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                    {
                        MyCamera.MV_GIGE_DEVICE_INFO_EX gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO_EX)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO_EX));

                        cbDeviceList.Add(gigeInfo.chSerialNumber);
                    }
                }
              
              
            }
           
            if (m_stDeviceList.nDeviceNum != 0)
            {
                SelectedItem = cbDeviceList[0]; // 默认选中第一个项目
            }
        }
        private void CapturingImageCommandExecute(object parameter)
        {
            if (m_stDeviceList.nDeviceNum == 0)
            {
                MessageBox.Show("未选择任何相机！");
                return;
            }

            int CameraIndex = cbDeviceList.Select((item, i) => new { item, i })
                         .FirstOrDefault(x => x.item == SelectedItem)
                         ?.i ?? -1;
            // ch:获取选择的设备信息 | en:Get selected device information

            if (CameraIndex < 0)
            {
                MessageBox.Show("该相机不存在");
            }
            
            MyCamera.MV_CC_DEVICE_INFO device =
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[CameraIndex],
                                                              typeof(MyCamera.MV_CC_DEVICE_INFO));

            // ch:打开设备 | en:Open device
            if (null == m_MyCamera)
            {
                m_MyCamera = new MyCamera();
                if (null == m_MyCamera)
                {
                    MessageBox.Show("打开相机失败！");
                    return;
                }
            }

            int nRet = m_MyCamera.MV_CC_CreateDevice_NET(ref device);
           

            nRet = m_MyCamera.MV_CC_OpenDevice_NET();
           

            // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = m_MyCamera.MV_CC_GetOptimalPacketSize_NET();
                m_MyCamera.MV_CC_SetIntValueEx_NET("GevSCPSPacketSize", nPacketSize);
                
            }
            //初始化图像
            SetImageSize();
            // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
            m_MyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            m_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

            m_stFrameInfo.nFrameLen = 0;//取流之前先清除帧长度
            m_stFrameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;

            m_hReceiveThread = new Thread(ReceiveThreadProcess);
            m_hReceiveThread.Start();

            // ch:开始采集 | en:Start Grabbing
            nRet = m_MyCamera.MV_CC_StartGrabbing_NET();
            bGrabbing = true;
        }

        private void StopCapturingCommandExecute(object parameter)
        {

        }
        public void ReceiveThreadProcess()
        {
            MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
            MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();
            MyCamera.MV_PIXEL_CONVERT_PARAM stConvertInfo = new MyCamera.MV_PIXEL_CONVERT_PARAM();
            int nRet = MyCamera.MV_OK;

            while (bGrabbing)
            {
                nRet = m_MyCamera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 1000);
                if (nRet == MyCamera.MV_OK)
                {
                    m_stFrameInfo = stFrameInfo.stFrameInfo;
                   
                    m_bitmap.Lock();
                    //拷贝内存
                    CopyMemory(m_bitmap.BackBuffer, stFrameInfo.pBufAddr, stFrameInfo.stFrameInfo.nFrameLen);
                    m_bitmap.Unlock();
                    m_bitmap.AddDirtyRect(new Int32Rect(0, 0, ImageWidth, ImageHeight));
                    //更新图像
                    //Application.Current.Dispatcher.Invoke(() => {
                    //    ImageSource = m_bitmap;
                    //});


                    m_MyCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }
        // ch:像素类型是否为Mono格式 | en:If Pixel Type is Mono 
      
      


        // ch:取图前的必要操作步骤 | en:Necessary operation before grab
        private void SetImageSize()
        {
            // ch:取图像宽 | en:Get Iamge Width
            MyCamera.MVCC_INTVALUE_EX stWidth = new MyCamera.MVCC_INTVALUE_EX();
            m_MyCamera.MV_CC_GetIntValueEx_NET("Width", ref stWidth);
            ImageWidth = Convert.ToInt32(stWidth.nCurValue);
            // ch:取图像高 | en:Get Iamge Height
            MyCamera.MVCC_INTVALUE_EX stHeight = new MyCamera.MVCC_INTVALUE_EX();
             m_MyCamera.MV_CC_GetIntValueEx_NET("Height", ref stHeight);
            ImageHeight = Convert.ToInt32(stHeight.nCurValue);

            m_bitmap = new WriteableBitmap(ImageWidth, ImageHeight, 96,96, PixelFormats.Gray8, null);



        }

        private void SetTriggerMode(TriggerMode mode)
        {
            if (mode == TriggerMode.SoftTware)
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
            }
            else
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", 2);

            }
        }

        private void SetCaptureMode( CaptureMode mode)
        {
            if (mode == CaptureMode.Continuous)
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            }
            else
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_SINGLE);
            }
        }
        //电气控制需要哪些功能，1.精确控制电机移动2.按照预定轨道移动3.移动到位后触发相机
        //加载模板2.执行匹配3.抓边计算旋转角度4.生成变换矩阵5.生成卡尺6.测量边的位置
       
   
    }
}
