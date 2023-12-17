using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{

    public class CheckAndComboModel : ComboModelBase
    {
        private bool? _IsCheck;
        public bool? IsCheck
        {
            get { return _IsCheck; }
            set { SetProperty(ref _IsCheck, value); }
        }
    }
}
