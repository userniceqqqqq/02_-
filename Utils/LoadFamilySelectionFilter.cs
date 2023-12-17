using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class LoadFamilySelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element ele)
        {
            if (!(ele is FamilyInstance))
            {
                return false;
            }
            FamilyInstance curInstance = ele as FamilyInstance;
            Family curFamily = curInstance.Symbol.Family;
            if (curFamily.IsInPlace || !curFamily.IsEditable || curInstance.Category == null)//排除内建族和不可编辑族——调用Document.EditFamily(family)时会抛出family，无法获取族文档
            {
                return false;
            }
            if (curInstance.Category.CategoryType != CategoryType.Model)
            {
                return false;
            }
            return true;
        }
        // 对于Reference，可通过构造函数传入document
        //public LoadFamilySelectionFilter(Document document)
        //{
        //    doc = document;
        //}
        //Document doc = null;
        public bool AllowReference(Reference reference, XYZ position)
        {
            //Element ele = doc.GetElement(reference);
            //if (!(ele is FamilyInstance))
            //{
            //    return false;
            //}
            //FamilyInstance curInstance = ele as FamilyInstance;
            //Family curFamily = curInstance.Symbol.Family;
            //if (curFamily.IsInPlace || !curFamily.IsEditable || curInstance.Category == null)//排除内建族和不可编辑族——调用Document.EditFamily(family)时会抛出family，无法获取族文档
            //{
            //    return false;
            //}
            //if (curInstance.Category.CategoryType != CategoryType.Model)
            //{
            //    return false;
            //}
            return false;
        }
    }
}
