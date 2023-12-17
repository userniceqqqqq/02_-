using Microsoft.Xaml.Behaviors;
using ParameterManager.Events;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using QiShiLog;
using QiShiLog.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using Unity;

namespace ParameterManager.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IRegionManager regionManager)
        {
            InitializeComponent();

            // 初始化TabControl——region           
            regionManager.RegisterViewWithRegion("MainContent", typeof(FamilyParameterContentView));
            regionManager.RegisterViewWithRegion("MainContent", typeof(OtherParameterContentView));
        }

        //~MainWindow()
        //{
        //    System.Diagnostics.Debug.WriteLine("MainWindow——析构");
        //}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 临时处理,若不关闭Revit，则无法释放Views实例
            Environment.Exit(0);//会连同Revit应用一起关闭——待测试：UI加载是否需要该方式                
        }
    }
}
