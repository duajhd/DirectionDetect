using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectionDetection.Shared
{
    public class Point2D
    {
        public double Row { get; set; }  // Y坐标
        public double Col { get; set; }  // X坐标

        public bool isDirectionCorrect {  get; set; }
        public Point2D(double row, double col, bool _isDirectionCorrect = false)
        {
            Row = row;
            Col = col;
           isDirectionCorrect = _isDirectionCorrect;
        }
    }
}
