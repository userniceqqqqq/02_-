using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class ParaKindAndComboModel : ComboModelBase
    {
        private ParaKindEnum _ParaKind;
        public ParaKindEnum ParaKind
        {
            get { return _ParaKind; }
            set { SetProperty(ref _ParaKind, value); }
        }
    }
}
