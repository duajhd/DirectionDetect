using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using MaterialDesignThemes.Wpf;
using MvCamCtrl.NET;
using HalconDotNet;
using MvCameraControl;
using static MvCamCtrl.NET.MyCamera;


namespace DirectionDetection.Camera
{
    //首先定义相机部分的功能
    public class HikCamera
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        private MyCamera m_pMyCamera;
        MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList;
        MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
        public  IntPtr m_pBufForDriver = IntPtr.Zero;
        UInt32 m_nBufSize = 0;
        string m_SerialNumber = string.Empty;
        private uint m_width = 0;
        private uint m_height = 0;
        private uint nOffsetX = 0;
        private uint nOffsetY = 0;

        bool IsFindCam = false;



        public HikCamera(string SerialNumber, uint Width, uint Height)
        {
            m_SerialNumber = SerialNumber;
            m_height = Height;
            m_width = Width;
        }
        public HikCamera(string SerialNumber, uint Width, uint Height, uint offsetX, uint offsetY)
        {
            m_SerialNumber = SerialNumber;
            m_height = Height;
            m_width = Width;
            nOffsetX = offsetX;
            nOffsetY = offsetY;
        }

        public bool HikInit()
        {
            int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE, ref m_pDeviceList);


            if (0 != nRet)
            {
                //ShowErrorMsg("Enumerate devices fail!", 0);
                return false;
            }

            // ch:获取选择的设备信息 | en:Get selected device information
            for (int i = 0; i < m_pDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[i],
                                                                 typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (MyCamera.MV_GIGE_DEVICE == device.nTLayerType)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chSerialNumber == m_SerialNumber)
                    {
                        IsFindCam = true;

                        if (null == m_pMyCamera)
                        {
                            m_pMyCamera = new MyCamera();
                            if (null == m_pMyCamera)
                            {
                                return false;
                            }
                        }
                        nRet = m_pMyCamera.MV_CC_CreateDevice_NET(ref device);
                        if (MyCamera.MV_OK != nRet)
                        {
                            return false;
                        }
                        break;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (!IsFindCam)
                return false;

            //MyCamera.MV_CC_DEVICE_INFO device =
            //    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[m_Id],
            //                                                  typeof(MyCamera.MV_CC_DEVICE_INFO));


            // ch:打开设备 | en:Open device
            nRet = m_pMyCamera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                m_pMyCamera.MV_CC_DestroyDevice_NET();
                m_pMyCamera = null;
                return false;
            }

            // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
            //      m_pMyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", 2);// ch:工作在连续模式 | en:Acquisition On Continuous Mode
            m_pMyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);    // ch:触发模式 | en:Continuous
            m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
            m_pMyCamera.MV_CC_SetIntValue_NET("Width", m_width);
            m_pMyCamera.MV_CC_SetIntValue_NET("Height", m_height);
            m_pMyCamera.MV_CC_SetIntValue_NET("OffsetX", nOffsetX);
            m_pMyCamera.MV_CC_SetIntValue_NET("OffsetY", nOffsetY);
            m_pMyCamera.MV_CC_SetBoolValue_NET("PacketUnorderSupport", true);
            m_pMyCamera.MV_CC_SetBoolValue_NET("AcquisitionFrameRateEnable", false);
            uint nPacketSize = (uint)m_pMyCamera.MV_CC_GetOptimalPacketSize_NET();
            m_pMyCamera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", nPacketSize);
            m_pMyCamera.MV_CC_StartGrabbing_NET();

            if (IntPtr.Zero == m_pBufForDriver)
            {
                MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
                UInt32 nPayloadSize = 0;

                nRet = m_pMyCamera.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
                if (0 == nRet)
                {
                    nPayloadSize = stParam.nCurValue;
                    m_pBufForDriver = Marshal.AllocHGlobal(Convert.ToInt32(m_width*m_height));
                    m_nBufSize = m_width * m_height;

                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public int  HikAcqImage(uint exposureTime,IntPtr pBufForDriver)
        {
            //Bitmap m_bitmap;
            //m_bitmap = new Bitmap(Convert.ToInt32(m_width), Convert.ToInt32(m_height), System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            //ColorPalette palette = m_bitmap.Palette;
            //for (int i = 0; i < 256; i++)
            //{
            //    palette.Entries[i] = System.Drawing.Color.FromArgb(255, i, i, i);
            //}
            //m_bitmap.Palette = palette;
            MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();

            if (exposureTime <= 80)
            {
                exposureTime = 2000;
            }
           

            int ret  =  m_pMyCamera.MV_CC_SetFloatValue_NET("ExposureTime", exposureTime);

            ret  = m_pMyCamera.MV_CC_SetCommandValue_NET("TriggerSoftware");
            if (MyCamera.MV_OK != ret)
            {
                return ret;
            }

            //按顺序执行MV_CC_ClearImageBuffer_NET和MV_CC_GetImageBuffer_NET很重要
            m_pMyCamera.MV_CC_ClearImageBuffer_NET();
            ret = m_pMyCamera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 20000);
            //
            if (MyCamera.MV_OK == ret)
            {
                //BitmapData bmpData = m_bitmap.LockBits(new Rectangle(0, 0, Convert.ToInt32(m_width), Convert.ToInt32(m_height)),
                //        ImageLockMode.WriteOnly,
                //        System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                //CopyMemory(bmpData.Scan0, stFrameInfo.pBufAddr, stFrameInfo.stFrameInfo.nFrameLen);
                CopyMemory(pBufForDriver, stFrameInfo.pBufAddr, stFrameInfo.stFrameInfo.nFrameLen);
              //  m_bitmap.Save("test.bmp");
                m_pMyCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);

            }
          

            return ret;
        }

        public bool HikAcqImagePtr(uint exposureTime)
        {
            if (exposureTime <= 80)
            {
                exposureTime = 2000;
            }
            Bitmap _bitmap = null;

            m_pMyCamera.MV_CC_SetFloatValue_NET("ExposureTime", exposureTime);

            int ret = m_pMyCamera.MV_CC_SetCommandValue_NET("TriggerSoftware");
            if (MyCamera.MV_OK != ret)
            {
                return false;
            }

            MyCamera.MV_FRAME_OUT_INFO_EX FrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

            m_pMyCamera.MV_CC_ClearImageBuffer_NET();

            ret = m_pMyCamera.MV_CC_GetOneFrameTimeout_NET(m_pBufForDriver, m_nBufSize, ref FrameInfo, 1000);
            // ret = m_pMyCamera.MV_CC_GetOneFrameEx_NET(
            if (MyCamera.MV_OK == ret)
            {
                return true;
            }
            return false;
        }

        public void HikClose()
        {
            if (null != m_pMyCamera)
            {
                m_pMyCamera.MV_CC_CloseDevice_NET();
                m_pMyCamera.MV_CC_DestroyDevice_NET();
            }
        }
    }

}
