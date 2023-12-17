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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ParameterManager.Views
{
    /// <summary>
    /// ParaImportDialogView.xaml 的交互逻辑
    /// </summary>
    public partial class ParaImportDialogView : UserControl
    {
        public ParaImportDialogView()
        {
            InitializeComponent();
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
    }
}
