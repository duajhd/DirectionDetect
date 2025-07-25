using System.Text;
using System.Windows;
using System.Collections.Concurrent;
using DirectionDetection.Views;
using DirectionDetection.Camera;
using DirectionDetection.PLC;
using DirectionDetection.Enums;
using System.Drawing;
using HalconDotNet;
using DirectionDetection.Shared;
using System.Windows.Media.Media3D;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Windows.Controls;
using EasyModbus;
using MvCamCtrl.NET;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
namespace DirectionDetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel viewModel;
        private  int DotSize = 50;
        private  int Spacing = 70;

        HikCamera cameraUp;
        ModbusClient client = new ModbusClient();

        private Thread cameraUp_process = null;
        //处理线程
        private Thread process1 = null;

        //是否正在运行标识
        bool isRunning = false;

        StateStep stateStep = StateStep.WaitPLC;

       

        private static uint ImageWidth = 5472;
        private static uint ImageHeight = 3648;

        

        //产品数量定义
        private int ProductRows = 22;
        private int ProductCols = 10;
        //每次拍摄视野内产品数定义
        private int RowStepNum = 5;
        private int ColStepNum = 2;
        //行列各需要的位移次数
        private int RowSteps;
        private int ColSteps;

     

        //根据位移次数判断本次矩阵大小
        //1.定时检测是否有产品2.
        private Point2D[,] totalResult = null;                        //总结果
        private Point2D[,]? singleResult = null;
        private int MovingNum = 5;

        //
        private int rowStep = 0;
        private int columnStep = 0;

        //当前总位移次数  totalMovingNum%RowSteps == 0表明进入新行totalMovingNum/RowSteps 表明是第几行，行号是偶数从左向右合并；奇数则从右向左合并
        private int totalMovingNum = 0;
        // Stack for temporary objects 
        HObject[] OTemp = new HObject[20];

        //图像更新标识
        private bool isUpdate = false;
        private object obj = new object();
        public IntPtr m_pBufForDriver = IntPtr.Zero;

        private bool isInitialized = false;

      
        //halcon相关
        // Local iconic variables 


      

        // Local iconic variables 

        HObject ho_img, ho_reg, ho_connectedReg, ho_SelectedRegions;
        HObject ho_Contours, ho_RightTopContour1, ho_LeftTopContour1;
        HObject ho_LeftBottomContour1, ho_RightBottomContour1, ho_Crosses2;
        HObject ho_line, ho_reducedImg;

        // Local control variables 

        HTuple hv_Width = new HTuple(), hv_Height = new HTuple();
        HTuple hv_MetrologyID = new HTuple(), hv_Row = new HTuple();
        HTuple hv_Column = new HTuple(), hv_phi = new HTuple();
        HTuple hv_Length1 = new HTuple(), hv_Length2 = new HTuple();
        HTuple hv_OffsetRowBegin = new HTuple(), hv_OffsetColumnBegin = new HTuple();
        HTuple hv_OffsetRowEnd = new HTuple(), hv_OffsetColumnEnd = new HTuple();
        HTuple hv_OffsetLeftTopRowBegin = new HTuple(), hv_OffsetLeftTopColumnBegin = new HTuple();
        HTuple hv_OffsetLeftTopRowEnd = new HTuple(), hv_OffsetLeftTopColumnEnd = new HTuple();
        HTuple hv_OffsetLeftBottomRowBegin = new HTuple(), hv_OffsetLeftBottomColumnBegin = new HTuple();
        HTuple hv_OffsetLeftBottomRowEnd = new HTuple(), hv_OffsetLeftBottomColumnEnd = new HTuple();
        HTuple hv_OffsetRightBottomRowBegin = new HTuple(), hv_OffsetRightBottomColumnBegin = new HTuple();
        HTuple hv_OffsetRightBottomRowEnd = new HTuple(), hv_OffsetRightBottomColumnEnd = new HTuple();
        HTuple hv_OffsetLineRowBegin = new HTuple(), hv_OffsetLineColumnBegin = new HTuple();
        HTuple hv_OffsetLineRowEnd = new HTuple(), hv_OffsetLineColumnEnd = new HTuple();
        HTuple hv_Index1 = new HTuple(), hv_MeasureRow = new HTuple();
        HTuple hv_MeasureColumn = new HTuple(), hv_Parameter = new HTuple();
        HTuple hv_RightTopContourRow1 = new HTuple(), hv_RightTopContourCol1 = new HTuple();
        HTuple hv_RightTopContourRow2 = new HTuple(), hv_RightTopContourCol2 = new HTuple();
        HTuple hv_Nr = new HTuple(), hv_Nc = new HTuple(), hv_Dist = new HTuple();

        //复位
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            client.WriteSingleCoil(0,true);
        }

        //初始化
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //如果未初始化，初始化
            if (!isInitialized)
            {
                try
                {
                    //

                    int[] res = client.ReadHoldingRegisters(303, 1);
                    if (res[0] != 1)
                    {
                        client.WriteSingleRegister(302, 1);
                    }

                    totalMovingNum = 0;
                    totalResult = null;
                    singleResult = null;
                    
                    isInitialized = true;
                    StartingBtn.IsEnabled = true;

                    InitialBtn.Content = "停止";

                    //启动线程部分
                    isRunning = true;
                    cameraUp_process = new Thread(work_flow_1);
                    process1 = new Thread(process);

                    cameraUp_process.Start();
                    process1.Start();
                }
                catch
                {
                    StartingBtn.IsEnabled = false;
                    isInitialized = false;
                    cameraUp.HikClose();
                }
            }
            //
            else
            {
                //已初始化，直接返回不处理
                isRunning = false;
                isInitialized = false;
                StartingBtn.IsEnabled = false;

                InitialBtn.Content = "初始化";
                cameraUp.HikClose();
            }


            //if (!isRunning)
            //{
            //    try
            //    {
            //        StartingBtn.Content = "停止";



            //        isRunning = true;

            //    }
            //    catch
            //    {
            //        isRunning = false;
            //    }


            //}
            //else
            //{
            //    StartingBtn.Content = "开始";
            //    client.WriteSingleRegister(300, 0);

            //   // cameraUp.HikClose();
            //}


        }

        HTuple hv_Index2 = new HTuple(), hv_LeftTopContourRow1 = new HTuple();
        HTuple hv_LeftTopContourCol1 = new HTuple(), hv_LeftTopContourRow2 = new HTuple();
        HTuple hv_LeftTopContourCol2 = new HTuple(), hv_Index3 = new HTuple();
        HTuple hv_LeftBottomContourRow1 = new HTuple(), hv_LeftBottomContourCol1 = new HTuple();
        HTuple hv_LeftBottomContourRow2 = new HTuple(), hv_LeftBottomContourCol2 = new HTuple();
        HTuple hv_Index4 = new HTuple(), hv_RightBottomContourRow1 = new HTuple();
        HTuple hv_RightBottomContourCol1 = new HTuple(), hv_RightBottomContourRow2 = new HTuple();
        HTuple hv_RightBottomContourCol2 = new HTuple(), hv_Row1_mid = new HTuple();
        HTuple hv_Column1_mid = new HTuple(), hv_Row2_mid = new HTuple();
        HTuple hv_Column2_mid = new HTuple(), hv_Mean = new HTuple();
        HTuple hv_Deviation = new HTuple();

        //点击开始执行
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            totalMovingNum = 0;
            totalResult = null;
            singleResult = null;
            client.WriteSingleRegister(300, 1);

            //if (!isRunning)
            //{
            //    try
            //    {
            //        StartingBtn.Content = "停止";



            //        isRunning = true;

            //    }
            //    catch
            //    {
            //        isRunning = false;
            //    }


            //}
            //else
            //{
            //    StartingBtn.Content = "开始";
            //    client.WriteSingleRegister(300, 0);

            //   // cameraUp.HikClose();
            //}

            //HikCamera vamer = new HikCamera("DA6063679", ImageWidth, ImageHeight);
            //vamer.HikInit();
            //vamer.HikAcqImage(38000, m_pBufForDriver);
            //vamer.HikClose();


            //  totalMovingNum += 1;
            //   HOperatorSet.GenImage1(out ho_img, "byte", ImageWidth, ImageHeight, m_pBufForDriver);
            //  HOperatorSet.ReadImage(out ho_img, "Image_20250707093946088.bmp");
            // HOperatorSet.WriteImage(ho_img,"bmp",0,"test.bmp");


        }

        private void RunningPLC()
        {

        }



        

     

        static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);  // 最多允许1个线程访问
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainWindowViewModel();
            this.DataContext = viewModel;
            

            //cameraDown = new HikCamera("12345", ImageWidth, ImageHeight);

            try
            {
                client.Connect("192.168.1.88", 502);
                PLCState.Fill = System.Windows.Media.Brushes.Green;
            }
            catch
            {
                PLCState.Fill = System.Windows.Media.Brushes.Red;
            }


            //HikCamera vamer = new HikCamera("DA6063679", ImageWidth, ImageHeight);
            try
            {
                cameraUp = new HikCamera("DA6063679", ImageWidth, ImageHeight);
                cameraUp.HikInit();
                CameraState.Fill = System.Windows.Media.Brushes.Green;  
            }
            catch
            {
                CameraState.Fill = System.Windows.Media.Brushes.Red;
            }
 
            //vamer.HikAcqImage(38000, m_pBufForDriver);
           
            m_pBufForDriver = Marshal.AllocHGlobal(Convert.ToInt32(ImageWidth * ImageHeight));

            HOperatorSet.GenEmptyObj(out ho_img);
            HOperatorSet.GenEmptyObj(out ho_reg);
            HOperatorSet.GenEmptyObj(out ho_connectedReg);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_Contours);
            HOperatorSet.GenEmptyObj(out ho_RightTopContour1);
            HOperatorSet.GenEmptyObj(out ho_LeftTopContour1);
            HOperatorSet.GenEmptyObj(out ho_LeftBottomContour1);
            HOperatorSet.GenEmptyObj(out ho_RightBottomContour1);
            HOperatorSet.GenEmptyObj(out ho_Crosses2);
            HOperatorSet.GenEmptyObj(out ho_line);
            HOperatorSet.GenEmptyObj(out ho_reducedImg);

            //初始化相机队列

            //右上抓边偏移
            hv_OffsetRowBegin.Dispose();
            hv_OffsetRowBegin = -300;
            hv_OffsetColumnBegin.Dispose();
            hv_OffsetColumnBegin = 95;
            hv_OffsetRowEnd.Dispose();
            hv_OffsetRowEnd = -150;
            hv_OffsetColumnEnd.Dispose();
            hv_OffsetColumnEnd = 95;

            //左上抓边偏移
            hv_OffsetLeftTopRowBegin.Dispose();
            hv_OffsetLeftTopRowBegin = -80;
            hv_OffsetLeftTopColumnBegin.Dispose();
            hv_OffsetLeftTopColumnBegin = -155;
            hv_OffsetLeftTopRowEnd.Dispose();
            hv_OffsetLeftTopRowEnd = -80;
            hv_OffsetLeftTopColumnEnd.Dispose();
            hv_OffsetLeftTopColumnEnd = -115;

            //左下抓边偏移
            hv_OffsetLeftBottomRowBegin.Dispose();
            hv_OffsetLeftBottomRowBegin = 300;
            hv_OffsetLeftBottomColumnBegin.Dispose();
            hv_OffsetLeftBottomColumnBegin = -90;
            hv_OffsetLeftBottomRowEnd.Dispose();
            hv_OffsetLeftBottomRowEnd = 150;
            hv_OffsetLeftBottomColumnEnd.Dispose();
            hv_OffsetLeftBottomColumnEnd = -90;
            //hv_RightBottomContourRow1
            //右下抓边偏移
            hv_OffsetRightBottomRowBegin.Dispose();
            hv_OffsetRightBottomRowBegin = 80;
            hv_OffsetRightBottomColumnBegin.Dispose();
            hv_OffsetRightBottomColumnBegin = 115;
            hv_OffsetRightBottomRowEnd.Dispose();
            hv_OffsetRightBottomRowEnd = 80;
            hv_OffsetRightBottomColumnEnd.Dispose();
            hv_OffsetRightBottomColumnEnd = 155;

          
            //检测线偏移
            hv_OffsetLineRowBegin.Dispose();
            hv_OffsetLineRowBegin = -30;
            hv_OffsetLineColumnBegin.Dispose();
            hv_OffsetLineColumnBegin = 129.06;
            hv_OffsetLineRowEnd.Dispose();
            hv_OffsetLineRowEnd = 40;
            hv_OffsetLineColumnEnd.Dispose();
            hv_OffsetLineColumnEnd = 129.06;

            hv_Width.Dispose(); hv_Height.Dispose();
            hv_MetrologyID.Dispose();
            hv_Width = ImageWidth;
            hv_Height = ImageHeight;
            HOperatorSet.CreateMetrologyModel(out hv_MetrologyID);
            HOperatorSet.SetMetrologyModelImageSize(hv_MetrologyID, hv_Width, hv_Height);

            RowSteps = (ProductRows + RowStepNum - 1) / RowStepNum;
            ColSteps = (ProductCols + ColStepNum - 1) / ColStepNum;

        

            //totalResult = new Point2D[ProductRows, ProductCols];


        }

        //private void DrawDots()
        //{
        //    // 示例二维数组

        //    DotCanvas.Children.Clear();

        //    for (int row = 0; row < totalResult.GetLength(0); row++)
        //    {
        //        for (int col = 0; col < totalResult.GetLength(1); col++)
        //        {
        //            Ellipse dot = new Ellipse
        //            {
        //                Width = DotSize,
        //                Height = DotSize,
        //                Fill = totalResult[row, col].isDirectionCorrect ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red
        //            };
        //            Canvas.SetLeft(dot, col * Spacing);
        //            Canvas.SetTop(dot, row * Spacing);

        //            DotCanvas.Children.Add(dot);


        //        }
        //    }
        //}


        private void DrawDots()
        {
            DotCanvas.Children.Clear();

            for (int row = 0; row < totalResult.GetLength(0); row++)
            {
                for (int col = 0; col < totalResult.GetLength(1); col++)
                {
                    // 计算位置
                    double left = col * Spacing;
                    double top = row * Spacing;

                    // 创建圆点
                    Ellipse dot = new Ellipse
                    {
                        Width = DotSize,
                        Height = DotSize,
                        Fill = totalResult[row, col].isDirectionCorrect ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red
                    };
                    Canvas.SetLeft(dot, left);
                    Canvas.SetTop(dot, top);
                    DotCanvas.Children.Add(dot);

                    // 创建文字
                    TextBlock text = new TextBlock
                    {
                        Text = $"{row+1},{col+1}",
                        FontSize = DotSize * 0.4,  // 文字大小适应点的尺寸
                        Foreground = System.Windows.Media.Brushes.White,
                        Width = DotSize,
                        Height = DotSize,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    // 手动定位文字（WPF 的 Canvas 不能自动居中对齐，所以手动偏移）
                    Canvas.SetLeft(text, left);
                    Canvas.SetTop(text, top + (DotSize - text.FontSize) / 2 - 1); // 微调垂直对齐
                    DotCanvas.Children.Add(text);
                }
            }
        }

        //if (!data[row, col])
        //{
        //    rowss.Text = (row + 1).ToString();
        //    colss.Text = (col + 1).ToString();
        //}
        private void parametersetting_Click(object sender, RoutedEventArgs e)
        {

            ParameterSetting parameterSetting = new ParameterSetting();
            parameterSetting.Show();
        }
       
        private void work_flow_1()
        {
            
            while (isRunning)
            {
                switch (stateStep)
                {
                    case StateStep.WaitPLC:
                        //D:100 == 1为触发
                        int[] res = client.ReadHoldingRegisters(100,1);
                        //触发信号且已经处理完成
                        if (res[0] == 1)
                        {
                            //线程同步
                            lock (obj)
                            {
                                if (!isUpdate)
                                {
                                    int ret = cameraUp.HikAcqImage(m_pBufForDriver);
                                    if (ret == 0)
                                    {
                                      
                                        isUpdate = true;
                                        //触发完成信号
                                        client.WriteSingleRegister(200, 1);

                                    }
                                }
                            }
                           


                        }
                        break;
                }

                Thread.Sleep(50);
            }
        }
        //public static Point2D[,] MergePoint2DArraysVerticalFlexibleSafe(Point2D[,] top, Point2D[,] bottom, bool insertTop)
        //{
        //    if (top == null && bottom == null)
        //        return new Point2D[0, 0];

        //    if (top == null)
        //        return ClonePoint2DArray(bottom);
        //    if (bottom == null)
        //        return ClonePoint2DArray(top);

        //    int rowsTop = top.GetLength(0);
        //    int colsTop = top.GetLength(1);
        //    int rowsBottom = bottom.GetLength(0);
        //    int colsBottom = bottom.GetLength(1);

        //    if (colsTop != colsBottom)
        //        throw new ArgumentException("两个数组的列数必须相同");

        //    int totalRows = rowsTop + rowsBottom;
        //    int cols = colsTop;
        //    Point2D[,] result = new Point2D[totalRows, cols];

        //    if (insertTop)
        //    {
        //        // top 在下方（先插 bottom 再插 top）
        //        for (int i = 0; i < rowsBottom; i++)
        //            for (int j = 0; j < cols; j++)
        //                result[i, j] = bottom[i, j];

        //        for (int i = 0; i < rowsTop; i++)
        //            for (int j = 0; j < cols; j++)
        //                result[i + rowsBottom, j] = top[i, j];
        //    }
        //    else
        //    {
        //        // top 在上方（先插 top 再插 bottom）
        //        for (int i = 0; i < rowsTop; i++)
        //            for (int j = 0; j < cols; j++)
        //                result[i, j] = top[i, j];

        //        for (int i = 0; i < rowsBottom; i++)
        //            for (int j = 0; j < cols; j++)
        //                result[i + rowsTop, j] = bottom[i, j];
        //    }

        //    return result;
        //}
        //逐渐扩张行2.
      
        public static Point2D[,] MergePoint2DArraysVerticalFlexibleSafe(Point2D[,] baseArray, Point2D[,] toInsert, bool insertTop)
        {
            // 特殊情况处理
            if (baseArray == null && toInsert == null)
                return new Point2D[0, 0];

            if (baseArray == null)
                return ClonePoint2DArray(toInsert);

            if (toInsert == null)
                return ClonePoint2DArray(baseArray);

            int baseRows = baseArray.GetLength(0);
            int baseCols = baseArray.GetLength(1);
            int insertRows = toInsert.GetLength(0);
            int insertCols = toInsert.GetLength(1);

            if (baseCols != insertCols)
                throw new ArgumentException("两个数组的列数必须相同");

            int totalRows = baseRows + insertRows;
            Point2D[,] result = new Point2D[totalRows, baseCols];

            if (insertTop)
            {
                // baseArray 在前，toInsert 在后
                for (int i = 0; i < baseRows; i++)//baseArray 
                                                  //insertArray                  
                    for (int j = 0; j < baseCols; j++)
                        result[i, j] = baseArray[i, j];

                for (int i = 0; i < insertRows; i++)
                    for (int j = 0; j < baseCols; j++)
                        result[i + baseRows, j] = toInsert[i, j];
            }
            else
            {
                // toInsert 在前，baseArray 在后
                for (int i = 0; i < insertRows; i++)   //insert
                                                       //baseArray
                    for (int j = 0; j < baseCols; j++)
                        result[i, j] = toInsert[i, j];

                for (int i = 0; i < baseRows; i++)
                    for (int j = 0; j < baseCols; j++)
                        result[i + insertRows, j] = baseArray[i, j];
            }

            return result;
        }

        //王世昌
        // 辅助方法：克隆一个二维数组
        private static Point2D[,] ClonePoint2DArray(Point2D[,] source)
        {
            if (source == null)
                return new Point2D[0, 0];

            int rows = source.GetLength(0);
            int cols = source.GetLength(1);
            Point2D[,] clone = new Point2D[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Point2D pt = source[i, j];
                    if (pt != null)
                        clone[i, j] = new Point2D(pt.Row, pt.Col, pt.isDirectionCorrect);
                    else
                        clone[i, j] = null;
                }
            }

            return clone;
        }

        //private static Point2D[,] ClonePoint2DArray(Point2D[,] source)
        //{
        //    if (source == null)
        //        return new Point2D[0, 0];

        //    int rows = source.GetLength(0);
        //    int cols = source.GetLength(1);
        //    Point2D[,] clone = new Point2D[rows, cols];

        //    for (int i = 0; i < rows; i++)
        //        for (int j = 0; j < cols; j++)
        //        {
        //            Point2D pt = source[i, j];
        //            if (pt != null)
        //                clone[i, j] = new Point2D(pt.Row, pt.Col, pt.isDirectionCorrect);
        //            else
        //                clone[i, j] = null;
        //        }

        //    return clone;
        //}

        //1.使用该函数明确哪个是左数组，哪个是右数组
        //public static Point2D[,] MergePoint2DArraysHorizontalFlexibleSafe(Point2D[,] left, Point2D[,] right, bool insertLeft)
        //{
        //    // 处理 null 情况
        //    if (left == null && right == null)
        //        return new Point2D[0, 0];

        //    if (left == null)
        //        return ClonePoint2DArray(right);
        //    if (right == null)
        //        return ClonePoint2DArray(left);

        //    int rowsLeft = left.GetLength(0);
        //    int colsLeft = left.GetLength(1);
        //    int rowsRight = right.GetLength(0);
        //    int colsRight = right.GetLength(1);

        //    if (rowsLeft != rowsRight)
        //        throw new ArgumentException("两个数组的行数必须相同");

        //    int totalCols = colsLeft + colsRight;
        //    Point2D[,] result = new Point2D[rowsLeft, totalCols];

        //    for (int i = 0; i < rowsLeft; i++)
        //    {
        //       //先放右数组，再放左数组
        //        if (insertLeft)
        //        {
        //            for (int j = 0; j < colsRight; j++)
        //                result[i, j] = right[i, j];
        //            for (int j = 0; j < colsLeft; j++)
        //                result[i, j + colsRight] = left[i, j];
        //        }
        //       //先放左数组，
        //        else
        //        {
        //            for (int j = 0; j < colsLeft; j++)
        //                result[i, j] = left[i, j];
        //            for (int j = 0; j < colsRight; j++)
        //                result[i, j + colsLeft] = right[i, j];
        //        }
        //    }

        //    return result;
        //}

        //public static Point2D[,] MergePoint2DArraysHorizontalFlexibleSafe(Point2D[,] baseArray, Point2D[,] insertArray, bool insertLeft)
        //{
        //    // 处理 null 情况
        //    if (baseArray == null && insertArray == null)
        //        return new Point2D[0, 0];

        //    if (baseArray == null)
        //        return ClonePoint2DArray(insertArray);

        //    if (insertArray == null)
        //        return ClonePoint2DArray(baseArray);

        //    int baseRows = baseArray.GetLength(0);
        //    int baseCols = baseArray.GetLength(1);
        //    int insertRows = insertArray.GetLength(0);
        //    int insertCols = insertArray.GetLength(1);

        //    if (baseRows != insertRows)
        //        throw new ArgumentException("两个数组的行数必须相同");

        //    int totalCols = baseCols + insertCols;
        //    Point2D[,] result = new Point2D[baseRows, totalCols];

        //    for (int i = 0; i < baseRows; i++)
        //    {
        //        if (insertLeft)
        //        {
        //            // 插入数组在左，基数组在右
        //            for (int j = 0; j < insertCols; j++)
        //                result[i, j] = insertArray[i, j];

        //            for (int j = 0; j < baseCols; j++)
        //                result[i, j + insertCols] = baseArray[i, j];
        //        }
        //        else
        //        {
        //            // 基数组在左，插入数组在右
        //            for (int j = 0; j < baseCols; j++)
        //                result[i, j] = baseArray[i, j];

        //            for (int j = 0; j < insertCols; j++)
        //                result[i, j + baseCols] = insertArray[i, j];
        //        }
        //    }

        //    return result;
        //}

        public static Point2D[,] MergePoint2DArraysHorizontalFlexibleSafe(Point2D[,] baseArray, Point2D[,] insertArray, bool insertLeft)
        {
            // 处理 null 情况
            if (baseArray == null && insertArray == null)
                return new Point2D[0, 0];

            if (baseArray == null)
                return ClonePoint2DArray(insertArray);

            if (insertArray == null)
                return ClonePoint2DArray(baseArray);

            int baseRows = baseArray.GetLength(0);
            int baseCols = baseArray.GetLength(1);
            int insertRows = insertArray.GetLength(0);
            int insertCols = insertArray.GetLength(1);

            if (baseRows != insertRows)
                throw new ArgumentException("两个数组的行数必须相同");

            int totalCols = baseCols + insertCols;
            Point2D[,] result = new Point2D[baseRows, totalCols];

            for (int i = 0; i < baseRows; i++)
            {
                if (insertLeft)
                {
                    // insertArray 在左边
                    for (int j = 0; j < insertCols; j++)
                        result[i, j] = insertArray[i, j];

                    for (int j = 0; j < baseCols; j++)
                        result[i, j + insertCols] = baseArray[i, j];
                }
                else
                {
                    // insertArray 在右边
                    for (int j = 0; j < baseCols; j++)          //
                        result[i, j] = baseArray[i, j];

                    for (int j = 0; j < insertCols; j++)
                        result[i, j + baseCols] = insertArray[i, j];
                }
            }

            return result;
        }
        //每一行第一个，将当前识别与null合并，该行其他向下合并（奇数）；偶数行则向上合并
        //每行最后一个，合并到单行以后，再将单行合并到总结果中


        public static Point2D[,] SpatialSort(List<Point2D> points, int rows, int cols, double rowTolerance = 20.0)
        {
            if (points.Count != rows * cols)
                throw new ArgumentException("点数量不等于 rows * cols");

            // 步骤 1：按 Y（Row）值排序
            var sortedByRow = points.OrderBy(p => p.Row).ToList();

            // 步骤 2：分组成“行”，每组是一个 List<Point2D>
            List<List<Point2D>> rowGroups = new List<List<Point2D>>();

            foreach (var point in sortedByRow)
            {
                // 找到可以归入的行（与第一点的 Y 差值小于容差）
                var row = rowGroups.FirstOrDefault(g => Math.Abs(g[0].Row - point.Row) < rowTolerance);
                if (row != null)
                {
                    row.Add(point);
                }
                else
                {
                    rowGroups.Add(new List<Point2D> { point });
                }
            }

            // 步骤 3：每一行内按 X（Col）排序
            var fullOrderedList = rowGroups
                .OrderBy(g => g.Min(p => p.Row))        // 上到下
                .Select(g => g.OrderBy(p => p.Col))     // 左到右
                .SelectMany(g => g)
                .ToList();

            // 步骤 4：填充二维数组
            Point2D[,] result = new Point2D[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i, j] = fullOrderedList[i * cols + j];

            return result;
        }
        private void work_flow_2()
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

         

        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            // 在这里执行清理操作
            ho_img.Dispose();
            ho_reg.Dispose();
            ho_connectedReg.Dispose();
            ho_SelectedRegions.Dispose();
            ho_Contours.Dispose();
            ho_RightTopContour1.Dispose();
            ho_LeftTopContour1.Dispose();
            ho_LeftBottomContour1.Dispose();
            ho_RightBottomContour1.Dispose();
            ho_Crosses2.Dispose();
            ho_line.Dispose();
            ho_reducedImg.Dispose();

            hv_Width.Dispose();
            hv_Height.Dispose();
            hv_MetrologyID.Dispose();
            hv_Row.Dispose();
            hv_Column.Dispose();
            hv_phi.Dispose();
            hv_Length1.Dispose();
            hv_Length2.Dispose();
            hv_OffsetRowBegin.Dispose();
            hv_OffsetColumnBegin.Dispose();
            hv_OffsetRowEnd.Dispose();
            hv_OffsetColumnEnd.Dispose();
            hv_OffsetLeftTopRowBegin.Dispose();
            hv_OffsetLeftTopColumnBegin.Dispose();
            hv_OffsetLeftTopRowEnd.Dispose();
            hv_OffsetLeftTopColumnEnd.Dispose();
            hv_OffsetLeftBottomRowBegin.Dispose();
            hv_OffsetLeftBottomColumnBegin.Dispose();
            hv_OffsetLeftBottomRowEnd.Dispose();
            hv_OffsetLeftBottomColumnEnd.Dispose();
            hv_OffsetRightBottomRowBegin.Dispose();
            hv_OffsetRightBottomColumnBegin.Dispose();
            hv_OffsetRightBottomRowEnd.Dispose();
            hv_OffsetRightBottomColumnEnd.Dispose();
            hv_OffsetLineRowBegin.Dispose();
            hv_OffsetLineColumnBegin.Dispose();
            hv_OffsetLineRowEnd.Dispose();
            hv_OffsetLineColumnEnd.Dispose();
            hv_Index1.Dispose();
            hv_MeasureRow.Dispose();
            hv_MeasureColumn.Dispose();
            hv_Parameter.Dispose();
            hv_RightTopContourRow1.Dispose();
            hv_RightTopContourCol1.Dispose();
            hv_RightTopContourRow2.Dispose();
            hv_RightTopContourCol2.Dispose();
            hv_Nr.Dispose();
            hv_Nc.Dispose();
            hv_Dist.Dispose();
            hv_Index2.Dispose();
            hv_LeftTopContourRow1.Dispose();
            hv_LeftTopContourCol1.Dispose();
            hv_LeftTopContourRow2.Dispose();
            hv_LeftTopContourCol2.Dispose();
            hv_Index3.Dispose();
            hv_LeftBottomContourRow1.Dispose();
            hv_LeftBottomContourCol1.Dispose();
            hv_LeftBottomContourRow2.Dispose();
            hv_LeftBottomContourCol2.Dispose();
            hv_Index4.Dispose();
            hv_RightBottomContourRow1.Dispose();
            hv_RightBottomContourCol1.Dispose();
            hv_RightBottomContourRow2.Dispose();
            hv_RightBottomContourCol2.Dispose();
            hv_Row1_mid.Dispose();
            hv_Column1_mid.Dispose();
            hv_Row2_mid.Dispose();
            hv_Column2_mid.Dispose();
            hv_Mean.Dispose();
            hv_Deviation.Dispose();

          
        }

        private void process()
        {
            bool _isUpdate = false;
            while (isRunning)
            {
                //如果图像未更新，则跳出
                lock (obj)
                {
                    _isUpdate = isUpdate;
                    if (_isUpdate)
                    {
                        HOperatorSet.GenImage1(out ho_img, "byte", ImageWidth, ImageHeight, m_pBufForDriver);
                    }
                  
                }
               
                //未更新跳出本次循环
                if (!_isUpdate)
                {
                    Thread.Sleep(50);
                    continue;
                }
                else
                {//这里可能会切换线程导致totalMovingNum与图中对不上，确定有更新再加1才可以
                    totalMovingNum += 1;
                    try
                    {
                        ho_reg.Dispose();
                        HOperatorSet.Threshold(ho_img, out ho_reg, 0, 170);
                        ho_connectedReg.Dispose();
                        HOperatorSet.ClosingCircle(ho_reg, out ho_connectedReg, 10);
                        {
                            HObject ExpTmpOutVar_0;
                            HOperatorSet.Connection(ho_connectedReg, out ExpTmpOutVar_0);
                            ho_connectedReg.Dispose();
                            ho_connectedReg = ExpTmpOutVar_0;
                        }
                        ho_SelectedRegions.Dispose();
                        HOperatorSet.SelectShape(ho_connectedReg, out ho_SelectedRegions, (((new HTuple("area")).TupleConcat(
                            "anisometry")).TupleConcat("rb")).TupleConcat("ra"), "and", (((new HTuple(3000)).TupleConcat(
                            3)).TupleConcat(150)).TupleConcat(600), (((new HTuple(99999999)).TupleConcat(
                            6)).TupleConcat(250)).TupleConcat(900));


                        hv_Row.Dispose(); hv_Column.Dispose(); hv_phi.Dispose(); hv_Length1.Dispose(); hv_Length2.Dispose();
                        HOperatorSet.SmallestRectangle2(ho_SelectedRegions, out hv_Row, out hv_Column,
                            out hv_phi, out hv_Length1, out hv_Length2);

                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Index1.Dispose();
                            HOperatorSet.AddMetrologyObjectLineMeasure(hv_MetrologyID, hv_Row + hv_OffsetRowBegin,
                                hv_Column + hv_OffsetColumnBegin, hv_Row + hv_OffsetRowEnd, hv_OffsetColumnEnd + hv_Column,
                                70, 5, 2, 60, new HTuple(), new HTuple(), out hv_Index1);
                        }
                        ho_Contours.Dispose(); hv_MeasureRow.Dispose(); hv_MeasureColumn.Dispose();
                        HOperatorSet.GetMetrologyObjectMeasures(out ho_Contours, hv_MetrologyID, hv_Index1,
                            "negative", out hv_MeasureRow, out hv_MeasureColumn);
                        HOperatorSet.ApplyMetrologyModel(ho_img, hv_MetrologyID);
                        hv_Parameter.Dispose();
                        HOperatorSet.GetMetrologyObjectResult(hv_MetrologyID, hv_Index1, "all", "result_type",
                            "all_param", out hv_Parameter);
                        ho_RightTopContour1.Dispose();
                        HOperatorSet.GetMetrologyObjectResultContour(out ho_RightTopContour1, hv_MetrologyID,
                            hv_Index1, "all", 1);
                        hv_RightTopContourRow1.Dispose(); hv_RightTopContourCol1.Dispose(); hv_RightTopContourRow2.Dispose(); hv_RightTopContourCol2.Dispose(); hv_Nr.Dispose(); hv_Nc.Dispose(); hv_Dist.Dispose();
                        HOperatorSet.FitLineContourXld(ho_RightTopContour1, "tukey", -1, 0, 5, 2, out hv_RightTopContourRow1,
                            out hv_RightTopContourCol1, out hv_RightTopContourRow2, out hv_RightTopContourCol2,
                            out hv_Nr, out hv_Nc, out hv_Dist);
                        //[208, 137, 213, 90], [49, 327, 139, 307], [100, 201, 214, 89], [82, 318, 56, 375]
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Index2.Dispose();
                            HOperatorSet.AddMetrologyObjectLineMeasure(hv_MetrologyID, hv_Row + hv_OffsetLeftTopRowBegin,
                                hv_Column + hv_OffsetLeftTopColumnBegin, hv_Row + hv_OffsetLeftTopRowEnd, hv_OffsetLeftTopColumnEnd + hv_Column,
                                90, 5, 1, 30, (new HTuple("measure_transition")).TupleConcat("min_score"),
                                (new HTuple("negative")).TupleConcat(0.5), out hv_Index2);
                        }

                        //add_metrology_object_line_measure (MetrologyID, 213, 139, 214, 56, 90, 5, 1, 30, ['measure_transition', 'min_score'], ['negative', 0.5], Index2)
                        ho_Contours.Dispose(); hv_MeasureRow.Dispose(); hv_MeasureColumn.Dispose();
                        HOperatorSet.GetMetrologyObjectMeasures(out ho_Contours, hv_MetrologyID, hv_Index2,
                            "negative", out hv_MeasureRow, out hv_MeasureColumn);
                        HOperatorSet.ApplyMetrologyModel(ho_img, hv_MetrologyID);
                        hv_Parameter.Dispose();
                        HOperatorSet.GetMetrologyObjectResult(hv_MetrologyID, hv_Index2, "all", "result_type",
                            "all_param", out hv_Parameter);
                        ho_LeftTopContour1.Dispose();
                        HOperatorSet.GetMetrologyObjectResultContour(out ho_LeftTopContour1, hv_MetrologyID,
                            hv_Index2, "all", 1);
                        hv_LeftTopContourRow1.Dispose(); hv_LeftTopContourCol1.Dispose(); hv_LeftTopContourRow2.Dispose(); hv_LeftTopContourCol2.Dispose(); hv_Nr.Dispose(); hv_Nc.Dispose(); hv_Dist.Dispose();
                        HOperatorSet.FitLineContourXld(ho_LeftTopContour1, "tukey", -1, 0, 5, 2, out hv_LeftTopContourRow1,
                            out hv_LeftTopContourCol1, out hv_LeftTopContourRow2, out hv_LeftTopContourCol2,
                            out hv_Nr, out hv_Nc, out hv_Dist);

                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Index3.Dispose();
                            HOperatorSet.AddMetrologyObjectLineMeasure(hv_MetrologyID, hv_Row + hv_OffsetLeftBottomRowBegin,
                                hv_Column + hv_OffsetLeftBottomColumnBegin, hv_Row + hv_OffsetLeftBottomRowEnd,
                                hv_OffsetLeftBottomColumnEnd + hv_Column, 70, 5, 1, 30, (new HTuple("measure_transition")).TupleConcat(
                                "min_score"), (new HTuple("negative")).TupleConcat(0.5), out hv_Index3);
                        }
                        ho_Contours.Dispose(); hv_MeasureRow.Dispose(); hv_MeasureColumn.Dispose();
                        HOperatorSet.GetMetrologyObjectMeasures(out ho_Contours, hv_MetrologyID, hv_Index3,
                            "negative", out hv_MeasureRow, out hv_MeasureColumn);
                        HOperatorSet.ApplyMetrologyModel(ho_img, hv_MetrologyID);
                        hv_Parameter.Dispose();
                        HOperatorSet.GetMetrologyObjectResult(hv_MetrologyID, hv_Index3, "all", "result_type",
                            "all_param", out hv_Parameter);
                        ho_LeftBottomContour1.Dispose();
                        HOperatorSet.GetMetrologyObjectResultContour(out ho_LeftBottomContour1, hv_MetrologyID,
                            hv_Index3, "all", 1);
                        hv_LeftBottomContourRow1.Dispose(); hv_LeftBottomContourCol1.Dispose(); hv_LeftBottomContourRow2.Dispose(); hv_LeftBottomContourCol2.Dispose(); hv_Nr.Dispose(); hv_Nc.Dispose(); hv_Dist.Dispose();
                        HOperatorSet.FitLineContourXld(ho_LeftBottomContour1, "tukey", -1, 0, 5, 2, out hv_LeftBottomContourRow1,
                            out hv_LeftBottomContourCol1, out hv_LeftBottomContourRow2, out hv_LeftBottomContourCol2,
                            out hv_Nr, out hv_Nc, out hv_Dist);

                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Index4.Dispose();
                            HOperatorSet.AddMetrologyObjectLineMeasure(hv_MetrologyID, hv_Row + hv_OffsetRightBottomRowBegin,
                                hv_Column + hv_OffsetRightBottomColumnBegin, hv_Row + hv_OffsetRightBottomRowEnd,
                                hv_OffsetRightBottomColumnEnd + hv_Column, 90, 5, 2, 60, new HTuple(), new HTuple(),
                                out hv_Index4);
                        }
                        ho_Contours.Dispose(); hv_MeasureRow.Dispose(); hv_MeasureColumn.Dispose();
                        HOperatorSet.GetMetrologyObjectMeasures(out ho_Contours, hv_MetrologyID, hv_Index4,
                            "negative", out hv_MeasureRow, out hv_MeasureColumn);
                        HOperatorSet.ApplyMetrologyModel(ho_img, hv_MetrologyID);
                        hv_Parameter.Dispose();
                        HOperatorSet.GetMetrologyObjectResult(hv_MetrologyID, hv_Index4, "all", "result_type",
                            "all_param", out hv_Parameter);
                        ho_RightBottomContour1.Dispose();
                        HOperatorSet.GetMetrologyObjectResultContour(out ho_RightBottomContour1, hv_MetrologyID,
                            hv_Index4, "all", 1);
                        hv_RightBottomContourRow1.Dispose(); hv_RightBottomContourCol1.Dispose(); hv_RightBottomContourRow2.Dispose(); hv_RightBottomContourCol2.Dispose(); hv_Nr.Dispose(); hv_Nc.Dispose(); hv_Dist.Dispose();
                        HOperatorSet.FitLineContourXld(ho_RightBottomContour1, "tukey", -1, 0, 5, 2,
                            out hv_RightBottomContourRow1, out hv_RightBottomContourCol1, out hv_RightBottomContourRow2,
                            out hv_RightBottomContourCol2, out hv_Nr, out hv_Nc, out hv_Dist);
                        //hv_LeftBottomContourRow1
                        hv_Row1_mid.Dispose();
                       
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Row1_mid = (hv_RightTopContourRow1 + hv_LeftBottomContourRow1) / 2;
                        }
                        hv_Column1_mid.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Column1_mid = (hv_RightTopContourCol1 + hv_LeftBottomContourCol1) / 2;
                        }
                        //hv_RightBottomContourRow1
                        hv_Row2_mid.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Row2_mid = (hv_LeftTopContourRow1 + hv_RightBottomContourRow1) / 2;
                        }
                        hv_Column2_mid.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Column2_mid = (hv_LeftTopContourCol1 + hv_RightBottomContourCol1) / 2;
                        }

                        ho_Crosses2.Dispose();
                        HOperatorSet.GenCrossContourXld(out ho_Crosses2, hv_Row1_mid, hv_Column1_mid,
                            12, 0);
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_line.Dispose();
                            HOperatorSet.GenRegionLine(out ho_line, hv_Row1_mid + hv_OffsetLineRowBegin, hv_Column1_mid + hv_OffsetLineColumnBegin,
                                hv_Row1_mid + hv_OffsetLineRowEnd, hv_Column1_mid + hv_OffsetLineColumnEnd);
                        }

                        //gen_region_line (line2, Row1_mid+OffsetLineRowBegin+540, Column1_mid+OffsetLineColumnBegin, Row1_mid+OffsetLineRowEnd+540.5, Column1_mid+OffsetLineColumnEnd)

                        ho_reducedImg.Dispose();
                        HOperatorSet.ReduceDomain(ho_img, ho_line, out ho_reducedImg);
                        hv_Mean.Dispose(); hv_Deviation.Dispose();
                        HOperatorSet.Intensity(ho_line, ho_img, out hv_Mean, out hv_Deviation);


                        //     HOperatorSet.Intensity(ho_line, ho_img, out hv_Mean, out hv_Deviation);

                        //核心是算出来每次拍照视野内的产品行数和列数
                        //当前拍照的视野内产品数
                        int ArrRows = 5;
                        int ArrCols = 2;
                        //产品放上开始   奇数行判断第一个；偶数行判断最后一颗
                        //1.判断行的奇偶2.判断是第一个还是最后一个（totalMovingNum%ColSteps == 1新行开始 == 0一行结束 totalMovingNum/ColSteps为行号）3.
                        //偶数行1.2.3    还是整除直接保留，不能整除向上取整
                        //if (Math.Ceiling(Convert.ToDouble(totalMovingNum / ColSteps)) %2 == 0) //判断当前行数是否是偶数
                        //{
                        //    //偶数行，判断是否是行开始 举例4%3
                        //    if((totalMovingNum % ColSteps) == 1)                                
                        //    {
                        //        //如果每列产品数能整除视野内产品列数，返回自身；否则返回每列产品数对视野内产品列数求余
                        //        ArrCols = ((ProductCols % ColStepNum) == 0) ? ArrCols : (ProductCols % ColStepNum);
                        //    }
                        //}
                        //else
                        //{
                        //    //如果是奇数行，判断是否是行结束

                        //}
                        //if ((totalMovingNum% RowSteps)  == (RowSteps - 1))
                        //{
                        //    //如果到了最后一行，需重新计算产品有几行。可以整除返回自身，不能整除返回余数

                        //    ArrRows = ((ProductRows % RowStepNum) == 0) ? ArrRows : (ProductRows% RowStepNum);

                        //}
                        //if ((totalMovingNum%ColSteps) == (ColSteps - 1))
                        //{
                        //    ArrCols = ((ProductCols % ColStepNum) == 0) ? ArrCols : (ProductCols % ColStepNum);
                        //}

                        //20250704核心还是判断局部二维数据的row和col。方法是1.触发2.位移总数+1 3.计算当前行的奇偶已经是否是最后或第一个
                        //行的尾数和列的尾数都需要判断


                        //先计算列再计算行,核心只有行结束或行开始是需要计算的否则都是固定的；其次行只需要判断余数，因为没有z字
                        //判断行的奇偶性2.计算尾数列值
                        //如果是偶数行 设：totalMovingNum  = 3 ColSteps = 1 
                        if (Math.Ceiling(Convert.ToDouble((float)totalMovingNum / ColSteps)) % 2 == 0)
                        {
                            //偶数行，判断是否是行开始 举例4%3
                            if ((totalMovingNum % ColSteps) == 1)   // == 1说明 行开始
                            {
                                //如果每列产品数能整除视野内产品列数，返回自身；否则返回每列产品数对视野内产品列数求余
                                ArrCols = ((ProductCols % ColStepNum) == 0) ? ArrCols : (ProductCols % ColStepNum);
                            }
                        }
                        else
                        {
                            //奇数行判断是否是行结束 
                            if ((totalMovingNum % ColSteps) == 0) //==0说明行结束
                            {
                                //如果每列产品数能整除视野内产品列数，返回自身；否则返回每列产品数对视野内产品列数求余
                                ArrCols = ((ProductCols % ColStepNum) == 0) ? ArrCols : (ProductCols % ColStepNum);
                            }

                        }


                        //对于行
                        //如果是最后一行 设totalMovingNum == 11;ColSteps
                        if (Math.Ceiling(Convert.ToDouble((float)totalMovingNum / ColSteps)) == RowSteps)
                        {
                            ArrRows = ((ProductRows % RowStepNum) == 0) ? ArrRows : (ProductRows % RowStepNum);
                        }


                        double[] rows = hv_Row1_mid.ToDArr();
                        double[] columns = hv_Column1_mid.ToDArr();
                        double[] mean = hv_Mean.ToDArr();
                        List<Point2D> ponits = new List<Point2D>();
                        for (int i = 0; i < rows.Length; i++)
                        {
                            ponits.Add(new Point2D(rows[i], columns[i], mean[i] <= 66 ? true : false));
                        }
                        //col:递增 row:相等
                        //这里相当于执行了一次矩阵转置
                        Point2D[,] parts = SpatialSort(ponits, ArrCols, ArrRows, 150);
                        //如何计算每次拍照视野内的产品行数和列数


                        //判断一行结束2.合并到最终结果3.
                        //将单次识别结果合并到singlwResult2.如果到了一行末尾将singleResult合并到totalResult3.清空singleResult4.将当前测量结果插入到singleResult4.如果是一行的开头，计算singleResult的尺寸

                        //当前的逻辑也就是说，同样是判断是否到达行尾或行开始，但是默认除了行开始或行尾其他都是正常的，但其实不是这样的。到了最后一列，每次拍照是两个

                        //行开始，分配singleresult和part，并将当前识别结果合并到
                        if (Math.Ceiling(Convert.ToDouble((float)totalMovingNum / ColSteps)) % 2 == 0)
                        {
                            //偶数行向上合并

                            if ((totalMovingNum % RowSteps) == 1)
                            {
                                //如果是偶数行第一个，向上合并
                                singleResult = null;
                                singleResult = MergePoint2DArraysVerticalFlexibleSafe(singleResult, parts, false);

                            }
                            else if ((totalMovingNum % RowSteps) == 0)
                            {
                                //偶数行最后一个
                                singleResult = MergePoint2DArraysVerticalFlexibleSafe(singleResult, parts, false);
                                //从左向右合并
                                totalResult = MergePoint2DArraysHorizontalFlexibleSafe(totalResult, singleResult, false);
                            }
                            else
                            {
                                //既不是行开始也不是行结束，直接合并到singleResult
                                //MergePoint2DArraysHorizontalFlexibleSafe(ref singleResult, parts, false);
                                singleResult = MergePoint2DArraysVerticalFlexibleSafe(singleResult, parts, false);

                            }

                        }
                        else
                        {
                            //奇数行向下合并


                            if ((totalMovingNum % RowSteps) == 1)
                            {
                                //如果是奇数行第一个
                                singleResult = null;
                                singleResult = MergePoint2DArraysVerticalFlexibleSafe(singleResult, parts, true);

                            }
                            else if ((totalMovingNum % RowSteps) == 0)
                            {
                                //奇数行最后一个
                                singleResult = MergePoint2DArraysVerticalFlexibleSafe( singleResult, parts, true);
                                //从左向右合并
                                totalResult =  MergePoint2DArraysHorizontalFlexibleSafe( totalResult, singleResult, false);
                                if (totalMovingNum  == 25)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        DrawDots();
                                        
                                    });
                                    totalMovingNum = 0;
                                    totalResult = null;


                                }
                            }
                            else
                            {
                                //既不是行开始也不是行结束，直接合并到singleResult
                                //MergePoint2DArraysHorizontalFlexibleSafe(ref singleResult, parts, false);
                                singleResult =  MergePoint2DArraysVerticalFlexibleSafe(singleResult, parts, true);

                            }



                        }











                        //if (totalMovingNum != 1 && (totalMovingNum % 5 == 0))
                        //{
                        //    //已经换行，插入左边 1.合并到完整数组2.清空single3.当前测量结果插入到左边
                        //    totalResult = MergePoint2DArraysVerticalFlexibleSafe(ref totalResult, singleResult, false);
                        //    singleResult = null;
                        //    singleResult = MergePoint2DArraysHorizontalFlexibleSafe(parts, singleResult, true);


                        //}
                        //else
                        //{
                        //    //同一行内，插入右边
                        //    singleResult = MergePoint2DArraysHorizontalFlexibleSafe(singleResult, parts, false);
                        //}
                        //

                        //处理

                        lock (obj)
                        {
                            isUpdate = false;
                        }
                    }
                    catch(Exception ex)
                    {
                        cameraUp.HikClose();
                        MessageBox.Show($"{ex.Message}程序异常，需点击复位按钮重做！");
                        string filePath = @"example.txt";
                        

                        // 写入内容，如果文件不存在则创建，已存在则覆盖
                        File.WriteAllText(filePath, $"hv_RightTopContourRow1长度{hv_RightTopContourRow1.DArr.Length}\r\nhv_LeftBottomContourRow1长度{hv_LeftBottomContourRow1.DArr.Length}\r\nhv_RightTopContourCol1长度{hv_RightTopContourCol1.DArr.Length}\r\nhv_LeftBottomContourCol1长度{hv_LeftBottomContourCol1.DArr.Length}\r\nhv_LeftTopContourRow1长度{hv_LeftTopContourRow1.DArr.Length}\r\nhv_RightBottomContourRow1长度{hv_RightBottomContourRow1.DArr.Length}\r\nhv_LeftTopContourCol1长度{hv_LeftTopContourCol1.DArr.Length}\r\nhv_RightBottomContourCol1长度{hv_RightBottomContourCol1.DArr.Length}");
                        //   File.WriteAllText(filePath, $"");

                        HOperatorSet.WriteImage(ho_img, "bmp", 0, "error.bmp");
                    }
                 
                    //HOperatorSet.WriteImage();
                   

                    //处理结束，恢复为未更新状态
                  
                }
                Thread.Sleep(50);

            }
        }
    }
}