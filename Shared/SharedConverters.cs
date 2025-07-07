using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using DirectionDetection.Enums;
namespace DirectionDetection.Shared
{
    public class BooleanInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }

   

    public class StringToBooleanConverter : IValueConverter
    {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value != null && value.ToString() == (string)parameter;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (bool)value ? parameter : Binding.DoNothing;
        }
    }


    //public class BooleanToEnumOfTrigger : IValueConverter
    //{ 
    //    //将vm枚举转到Ischeck
    //    public object Convert(object value,Type targetType,object parameter, CultureInfo culture)
    //    {
    //        CameraMode mode = (CameraMode)value;


    //        return value != null&& mode.ToString() == (string)parameter;


    //    }
    //    //将IScheck转到枚举
    //    public object ConvertBack(object value ,Type targetType,object parameter, CultureInfo culture)
    //    {
    //        string Strmode = (string)parameter;
    //        CameraMode mode;
    //        if (Enum.TryParse(Strmode, out mode))
    //        {
    //            return (CameraMode)mode;
    //        }
    //        else
    //        {
    //            return  Binding.DoNothing;
    //        }
    //    }
    //}
    public class TriggerModeConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TriggerMode && parameter is string)
            {
                var mode = (TriggerMode)value;
                var modeName = parameter.ToString();

                if (modeName.Equals("SoftTware", StringComparison.OrdinalIgnoreCase))
                    return mode == TriggerMode.SoftTware;
                else if (modeName.Equals("IO", StringComparison.OrdinalIgnoreCase))
                    return mode == TriggerMode.IO;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value && parameter is string)
            {
                var modeName = parameter.ToString();
                if (modeName.Equals("SoftTware", StringComparison.OrdinalIgnoreCase))
                    return TriggerMode.SoftTware;
                else if (modeName.Equals("IO", StringComparison.OrdinalIgnoreCase))
                    return TriggerMode.IO;
            }
            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    //public class EnumToBooleanConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is CaptureMode mode)
    //        {
    //            return mode == CaptureMode.Trigger;
    //        }
    //        return false;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is bool isChecked && isChecked)
    //        {
    //            return CaptureMode.Trigger;
    //        }
    //        // 如果未选中，则可以返回 Continuous 或者不更改
    //        return Binding.DoNothing; // 或者返回 CameraMode.Continuous;
    //    }
    //}
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(Enum.Parse(value.GetType(), parameter.ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Enum.Parse(targetType, parameter.ToString()) : Binding.DoNothing;
        }
    }
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Brushes.Green : Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }



public class BoolToButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = (bool)value;
            return flag ? "停止" : "启动";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 一般按钮Text不做反向绑定，返回不处理
            throw new NotImplementedException();
        }
    }

}
