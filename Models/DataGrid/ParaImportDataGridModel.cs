using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ParameterManager
{
    public class ParaImportDataGridModel : BindableBase
    {
        private string _ParaName;
        public string ParaName
        {
            get { return _ParaName; }
            set { SetProperty(ref _ParaName, value); }
        }

        private string _GroupName;
        public string GroupName
        {
            get { return _GroupName; }
            set { SetProperty(ref _GroupName, value); }
        }

        private bool? _IsCheck = false;
        public bool? IsCheck //null表示未全选该节点的所有子节点
        {
            get { return _IsCheck; }
            set { SetProperty(ref _IsCheck, value); }
        }

        private bool _IsSelect = false;
        public bool IsSelect
        {
            get { return _IsSelect; }
            set { SetProperty(ref _IsSelect, value); }
        }

        private Visibility _NodeVisibility = Visibility.Visible;
        public Visibility NodeVisibility
        {
            get { return _NodeVisibility; }
            set { SetProperty(ref _NodeVisibility, value); }
        }
    }
}
