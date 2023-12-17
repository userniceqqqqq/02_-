using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    class ParaGroupAndComboModel : ComboModelBase
    {
        private BuiltInParameterGroup _ParaGroup;
        public BuiltInParameterGroup ParaGroup
        {
            get { return _ParaGroup; }
            set { SetProperty(ref _ParaGroup, value); }
        }


    }
}
