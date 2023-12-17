using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager.ViewModels
{
    class OtherParameterContentViewModel : BindableBase
    {
        public OtherParameterContentViewModel()
        {

        }

        private string _Title = "其他参数";
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }

        private string _DisplayValue = "其他参数";
        public string DisplayValue
        {
            get { return _DisplayValue; }
            set { SetProperty(ref _DisplayValue, value); }
        }

    }
}
