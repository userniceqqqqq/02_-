using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class GetUnitGroupToParameterType : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;

            Dictionary<UnitGroup, List<ParameterType>> dict = new Dictionary<UnitGroup, List<ParameterType>>();

            IList<UnitType> unitTypes = UnitUtils.GetValidUnitTypes();
            IList<ParameterType> parameterTypes = Enum.GetValues(typeof(ParameterType)).Cast<ParameterType>().ToList();

            foreach (UnitType unitType in unitTypes)
            {
                UnitGroup unitGroup = UnitUtils.GetUnitGroup(unitType);
                if (!dict.ContainsKey(unitGroup))
                {
                    dict.Add(unitGroup, new List<ParameterType>());
                }
                string unitTypeName = unitType.ToString().Substring(3).Replace("_", "").ToLower();
                var result = from item in parameterTypes
                             where item.ToString().ToLower().Equals(unitTypeName)
                             select item;
                if (result.Count() > 0)
                {
                    dict[unitGroup].Add(result.FirstOrDefault());
                }
            }

            // 手动添加不与UnitType匹配的ParameterType（共：12个——Invalid无法用于创建参数）
            dict[UnitGroup.Common].Add(ParameterType.URL);
            dict[UnitGroup.Common].Add(ParameterType.Text);
            dict[UnitGroup.Common].Add(ParameterType.MultilineText);
            dict[UnitGroup.Common].Add(ParameterType.Integer);
            dict[UnitGroup.Common].Add(ParameterType.Material);
            dict[UnitGroup.Common].Add(ParameterType.YesNo);
            dict[UnitGroup.Common].Add(ParameterType.Image);
            dict[UnitGroup.Common].Add(ParameterType.FamilyType);

            dict[UnitGroup.Electrical].Add(ParameterType.NumberOfPoles);
            dict[UnitGroup.Electrical].Add(ParameterType.LoadClassification);

            dict[UnitGroup.Piping].Add(ParameterType.FixtureUnit);

            //缓存
            SysCache.Instance.UnitGroupToParameterType = dict;
        }

        public string GetName()
        {
            return "GetUnitGroupToParameterType";
        }
    }
}
