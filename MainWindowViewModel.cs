using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using System.Windows.Input;
using System.Collections.ObjectModel;
using HalconDotNet;
namespace DirectionDetection
{
   
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
    //1.连接5个相机2.发出第一组相机采图3.采集图像放队列4.线程取图处理5.输出处理结果（坐标，ok,ng）合并到最终结果数组（不用单个图像块转换坐标），但还是得排序
    public class MainWindowViewModel:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            PropertyChanged.Invoke(this,new PropertyChangedEventArgs(PropertyName));
        }
        //测试图像采集，绘制ROI；2.核心是生成图像块号=》二维数组的映射（已知row、column，每个图像块的号，计算在整体数组的下标）
        //1.连接PLC和相机2.监听PLC的线圈
        public ConcurrentQueue<WriteableBitmap> ImageBlock { get; set; }




       
        //相机数

        public int CamerasCount;

        private int ProductRows;
        private int ProductColumns; 

        private int NumofSingleRow;

        private int NumofSingleColumn;


        //1.除以3，计算row坐标阈值 2.for(){
      


        //某个相机，图像号对应的整体数组的下标
       private List<List<int>> ImageBlockToIndex =  new List<List<int>>();

         public MainWindowViewModel()
         {
            //List<List<int>> test = new List<List<int>>();

            //float temp = 20.0f;

            //float start = 0.0f;
            //int rows = 3;
            //for (int i = 0;i<rows;i++)
            //{
            //    start += temp;
            //    test.Add(new List<int>());
            //    for (int j = 0;j<products;j++)
            //    {
            //        if (products[j].column<= start)
            //        {
            //            test[^1].Add(j);
            //        }
            //    }

            //}

            //1
            //执行完y
          
        
            }

        //rows: 内容是行序号  //productsOfColumn每行产品数   //
        //目标是计算图像块对应的下标

       
        private void CaculateIndex(int[] rows , int productsOfColumn)
        {
            int total = 6;
            //计算中心坐标，计算图片height。
            for (int i = 0;i< rows.Length;i++)
            {
                int[] row = new int[productsOfColumn];
                int[] column = new int[productsOfColumn];
                //area_center
                //int field = height/2;
                //for ()
                //{
                //    if (row[i]< field)
                //    {
                //        (rows[i],)
                //    }
                //    else
                //    {

                //    }
                //}

            }
        }

        private void InitMachine()
        {

        }



    //    class Point2D
    //    {
    //        public double Row { get; set; }
    //        public double Col { get; set; }

    //        public Point2D(double row, double col)
    //        {
    //            Row = row;
    //            Col = col;
    //        }
    //    }

    //    List<Point2D> centers = new List<Point2D>();
    //    for (int i = 0; i<rowCenters.Count; i++)
    //    {
    //        centers.Add(new Point2D(rowCenters[i], colCenters[i]));
    //    }

    //    // 1. 按 Row 分组（分三行）：
    //    double rowThreshold = 20.0; // 可根据实际情况调整
    //    var rowGroups = centers
    //        .GroupBy(p =>
    //            rowCenters.OrderBy(r => r)
    //                      .Select((val, idx) => new { val, idx })
    //                      .First(x => Math.Abs(x.val - p.Row) < rowThreshold).idx / 6
    //        )
    //        .OrderBy(g => g.Min(p => p.Row))
    //        .ToList();

    //    // 2. 每行内按列排序
    //    List<Point2D> ordered = new List<Point2D>();
    //    foreach (var row in rowGroups)
    //    {
    //        var sortedRow = row.OrderBy(p => p.Col).ToList();
    //ordered.AddRange(sortedRow);
    //    }

        // 现在 ordered 就是从左上角开始 0,1,...,17 的顺序

    }
}
