using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class LoadFamilyTreeSource : IExternalEventHandler
    {
        public LoadFamilyTreeSource()
        {
            familyList = SysCache.Instance.FamilyTreeSourceList;
        }
        private IList<Element> familyList;
        private Document doc;

        public void Execute(UIApplication app)
        {
            familyList.Clear();
            doc = app.ActiveUIDocument.Document;
            if (SysCache.Instance.CurFamily != null)
            {
                doc = doc.EditFamily(SysCache.Instance.CurFamily);
            }

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> listFamily = collector.OfClass(typeof(Family)).ToElements();

            foreach (var element in listFamily)
            {
                Family curFamily = element as Family;
                if (curFamily.IsInPlace || !curFamily.IsEditable)//排除内建族和不可编辑族——调用Document.EditFamily(family)时会抛出family，无法获取族文档
                {
                    continue;
                }
                foreach (var familySymbolId in curFamily.GetFamilySymbolIds())
                {
                    Element curFamilySymbolElement = doc.GetElement(familySymbolId);
                    FamilySymbol curFamilySymbol = curFamilySymbolElement as FamilySymbol;
                    if (curFamilySymbol.Category == null)
                    {
                        continue;
                    }
                    if (curFamilySymbol.Category.CategoryType == CategoryType.Model && !familyList.Contains(curFamilySymbolElement))
                    {
                        familyList.Add(curFamilySymbolElement);
                    }
                }
            }
            return;
        }

        public string GetName()
        {
            return "LoadFamilyTreeSource";
        }
    }
}
