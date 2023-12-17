using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class ParaTypeAndComboModel : ComboModelBase
    {
        private ParameterType _ParaType;
        public ParameterType ParaType
        {
            get { return _ParaType; }
            set { SetProperty(ref _ParaType, value); }
        }

    }
}
