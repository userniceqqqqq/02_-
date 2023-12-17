using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class BoolAndComboModel : ComboModelBase
    {
        private bool? _BoolValue;
        public bool? BoolValue
        {
            get { return _BoolValue; }
            set { SetProperty(ref _BoolValue, value); }
        }

        public string BindingName { get; set; }

    }
}
