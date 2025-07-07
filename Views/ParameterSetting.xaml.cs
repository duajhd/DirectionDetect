using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DirectionDetection.ViewModels;

namespace DirectionDetection.Views
{
    /// <summary>
    /// ParameterSetting.xaml 的交互逻辑
    /// </summary>
    public partial class ParameterSetting : Window
    {
        ParameterSettingViewModel vm;
        public ParameterSetting()
        {
            InitializeComponent();
            vm = new ParameterSettingViewModel();
            this.DataContext = vm;
        }
    }
}
