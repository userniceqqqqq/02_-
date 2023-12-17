using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class ParaCreateDataGridModel : BindableBase
    {
        private string _MarkName = Convert.ToString((char)8730);//"对号"
        public string MarkName
        {
            get { return _MarkName; }
            set { SetProperty(ref _MarkName, value); }
        }

        private string _FamilyName;
        public string FamilyName
        {
            get { return _FamilyName; }
            set { SetProperty(ref _FamilyName, value); }
        }

        private string _ParaName;
        public string ParaName
        {
            get { return _ParaName; }
            set { SetProperty(ref _ParaName, value); }
        }

        private string _Description = "该参数创建成功";
        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }

        private bool _IsCreatedSuccess = true;
        public bool IsCreatedSuccess
        {
            get { return _IsCreatedSuccess; }
            set { SetProperty(ref _IsCreatedSuccess, value, changedCreateSuccess); }
        }

        private void changedCreateSuccess()
        {
            if (!IsCreatedSuccess)
            {
                MarkName = Convert.ToString((char)215);//"叉号"
            }
        }
    }
}
