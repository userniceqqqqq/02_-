using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class DisciplineAndComboModel : ComboModelBase
    {
        private UnitGroup _Discipline;
        public UnitGroup Discipline
        {
            get { return _Discipline; }
            set { SetProperty(ref _Discipline, value); }
        }
    }
}
