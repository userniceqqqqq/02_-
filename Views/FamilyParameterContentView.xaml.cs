using Microsoft.Xaml.Behaviors;
using ParameterManager.Events;
using Prism.Events;
using Prism.Interactivity;
using Prism.Regions;
using QiShiLog;
using QiShiLog.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Unity;

namespace ParameterManager.Views
{
    /// <summary>
    /// FamilyParameterContentView.xaml 的交互逻辑
    /// </summary>
    public partial class FamilyParameterContentView : UserControl
    {
        public FamilyParameterContentView(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            eventAggregator.GetEvent<DynamicColumnEvent>().Subscribe(DynamicColumn, ThreadOption.UIThread);
        }

        //~FamilyParameterContentView()
        //{
        //    System.Diagnostics.Debug.WriteLine("FamilyParameterContentView——————析构");
        //}


        //动态列
        private void DynamicColumn(object args)
        {
            try
            {
                CheckAndComboModel curAttribute = args as CheckAndComboModel;
                AdditionPropertyEnum curAddProp = HelpEnum.GetEnumByDescription<AdditionPropertyEnum>(curAttribute.Name);
                string controlName = curAddProp.ToString();
                //删除列
                if (curAttribute.IsCheck == false)
                {
                    DataGridColumn existColumn = dataGrid.Columns.FirstOrDefault(x =>
                    {
                        if (x.Header == null)
                        {
                            return false;
                        }
                        return x.Header.Equals(curAttribute.Name);
                    });
                    dataGrid.Columns.Remove(existColumn);
                    return;
                }

                //增加列——两种情况：Text列、ComboBox列
                DataGridLength length = new DataGridLength(1, DataGridLengthUnitType.Star); //Width="*"后台写法
                if (curAddProp == AdditionPropertyEnum.Description)
                {
                    DataGridTextColumn textColumn = new DataGridTextColumn()
                    {
                        Header = curAttribute.Name,
                        Binding = new Binding(curAddProp.ToString()),
                        IsReadOnly = false,
                        Width = length
                    };
                    textColumn.CellStyle = (Style)this.Resources["DataGridCellFortText"];
                    dataGrid.Columns.Add(textColumn);
                    return;
                }

                // 扩展一：
                //this.DataContext;////获取xaml前台处上下文
                //DataTemplate dataTemplate = (DataTemplate)this.Resources["ComboBoxSelectTemplate"];//获取xaml前台处资源<UserControl.Resources>

                // 扩展二：后代代码动态创建列——自定义DataTemplate
                //【第二种~使用 XamlReader 类的 Load 方法从字符串或内存流中加载 XAML】略
                //【第二种~将FrameworkElementFactory对象设置为DataTemplate对象的VisualTree属性】
                //FrameworkElementFactory factory = new FrameworkElementFactory(typeof(ComboBox));
                //factory.SetBinding(ComboBox.ItemsSourceProperty, new Binding());
                //factory.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(ComboBox_SelectionChanged));
                //DataTemplate dataTemplate = new DataTemplate();
                //dataTemplate.VisualTree = factory;
                //【第三种~辅助类TemplateGenerator，通过委托创建】
                if (this.FindName(controlName) is DataGridTemplateColumn)
                {
                    DataGridTemplateColumn columnOld = this.FindName(controlName) as DataGridTemplateColumn;
                    dataGrid.Columns.Add(columnOld);
                    return;
                }
                DataGridTemplateColumn column = new DataGridTemplateColumn()
                {
                    Header = curAttribute.Name,
                    Width = length
                };
                this.RegisterName(controlName, column);
                string itemsSourceBindPath = null;
                switch (controlName)
                {
                    case "ParaVisibility":
                        itemsSourceBindPath = "DataContext." + "ParaVisibleModels";
                        break;
                    case "UserModifiable":
                        itemsSourceBindPath = "DataContext." + "IsModifiableModels";
                        break;
                    case "HideWhenNoValue":
                        itemsSourceBindPath = "DataContext." + "IsHideModels";
                        break;
                }
                DataTemplate dataTemplate = TemplateGenerator.CreateDataTemplate(() =>
                {
                    ComboBox comboBox = new ComboBox();
                    comboBox.SetBinding(ComboBox.ItemsSourceProperty, new Binding { Path = new PropertyPath(itemsSourceBindPath), RelativeSource = new RelativeSource { Mode = RelativeSourceMode.FindAncestor, AncestorType = typeof(UserControl) } });
                    comboBox.SetBinding(ComboBox.SelectedValueProperty, new Binding { Path = new PropertyPath(controlName), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                    comboBox.SetValue(ComboBox.DisplayMemberPathProperty, "Name");
                    comboBox.SetValue(ComboBox.SelectedValuePathProperty, "BoolValue");
                    comboBox.SetBinding(ComboBox.WidthProperty, new Binding { Path = new PropertyPath("ActualWidth"), ElementName = controlName });
                    comboBox.SetBinding(ComboBox.IsEnabledProperty, new Binding { Path = new PropertyPath("IsFamilyParameter"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                    // 后台代码实现事件转命令                                        
                    Prism.Interactivity.InvokeCommandAction invokeCommandAction = new Prism.Interactivity.InvokeCommandAction();
                    Binding binding = new Binding { Path = new PropertyPath("DataContext.SelectionChangedCommand"), RelativeSource = new RelativeSource { Mode = RelativeSourceMode.FindAncestor, AncestorType = typeof(UserControl) } };
                    BindingOperations.SetBinding(invokeCommandAction, Prism.Interactivity.InvokeCommandAction.CommandProperty, binding);

                    Microsoft.Xaml.Behaviors.EventTrigger eventTrigger = new Microsoft.Xaml.Behaviors.EventTrigger("SelectionChanged");
                    eventTrigger.Actions.Add(invokeCommandAction);

                    Microsoft.Xaml.Behaviors.TriggerCollection triggers = Interaction.GetTriggers(comboBox);
                    triggers.Add(eventTrigger);
                    comboBox.Style = (Style)this.Resources["ComboBoxForDataGridStyle"];
                    comboBox.Template = (ControlTemplate)this.Resources["ComboBoxForDataGridControlTemplate1"];

                    return comboBox;

                });
                column.CellTemplate = dataTemplate;

                Style style = new Style(typeof(DataGridCell));
                Setter setter = new Setter();
                setter.Property = DataGridCell.IsEnabledProperty;
                setter.Value = new Binding() { Path = new PropertyPath("DataContext.IsShareParameter"), Mode = BindingMode.OneWay, RelativeSource = new RelativeSource { Mode = RelativeSourceMode.FindAncestor, AncestorType = typeof(DataGridRow) } };
                style.Setters.Add(setter);

                style.Setters.Add(new Setter() { Property = DataGridCell.BorderThicknessProperty, Value = new Thickness(0) });
                Trigger trigger = new Trigger() { Property = DataGridCell.IsSelectedProperty, Value = true };
                BrushConverter brushConverter= new BrushConverter();
                trigger.Setters.Add(new Setter() { Property = DataGridCell.BackgroundProperty, Value = (Brush)brushConverter.ConvertFrom("#FF8EA2A4") });
                style.Triggers.Add(trigger);

                column.CellStyle = style;
                dataGrid.Columns.Add(column);
            }
            catch (Exception ex)
            {
                Logger.Instance.Info($"报错信息,{ex}");
                Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
            }
        }



        //禁用ComboBox鼠标滚轮
        private void ComboBoxItem_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        //禁用ComboBox自动滚动至下一条
        private void ComboBoxItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;//标记事件已经处理完
        }

        // 固定ComboBox滚动条至需要位置
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ScrollViewer scrollViewerCombo = comboBox.Template.FindName("DropDownScrollViewer", comboBox) as ScrollViewer;

            // 若设置了ComboBox的MaxDropDownHeight属性，需执行下面两行代码——否则可能会出现多余空白行——可能原因：存在多余高度，又不够一行高度
            scrollViewerCombo.CanContentScroll = false;
            scrollViewerCombo.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

            //待日后解决——为什么ScrollToTop、LineUp等向上滚动及item.BringIntoView()操作不起作用，向下操作（ScrollToBottom、LineDown、PageDown）才起作用
            scrollViewerCombo.ScrollToBottom();
        }

        #region 无效代码——无法释放该对象内存
        //public void Dispose()
        //{
        //    GC.Collect();
        //    GC.SuppressFinalize(this);
        //    System.Diagnostics.Debug.WriteLine("FamilyParameterContentView——————Dispose");
        //    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        //    {
        //        SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
        //    }
        //}
        //[System.Runtime.InteropServices.DllImportAttribute("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
        //private static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize); 

        #endregion      
    }
}
