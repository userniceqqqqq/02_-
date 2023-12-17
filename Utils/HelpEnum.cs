using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class HelpEnum
    {

        /// <summary>
        /// 根据枚举型获取指定类型的Model实例
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="curEnum"></param>
        /// <returns></returns>
        public static object GetModel<TEnum, TModel>(TEnum curEnum) where TEnum : Enum where TModel : new()
        {
            HelpAttribute attribute;
            FieldInfo field = typeof(TEnum).GetField(curEnum.ToString());
            object[] attributes = field.GetCustomAttributes(typeof(HelpAttribute), false);
            if (attributes == null || attributes.Length == 0)
            {
                return null;
            }
            foreach (var item in attributes)
            {
                if (item is HelpAttribute)
                {
                    attribute = item as HelpAttribute;

                    TModel curModel = new TModel();
                    PropertyInfo[] itemPropertys = typeof(TModel).GetProperties();
                    PropertyInfo[] attributePropertys = typeof(HelpAttribute).GetProperties();
                    foreach (var curprop in itemPropertys)
                    {
                        if (curprop.Name == "ParaKind")
                        {
                            curprop.SetValue(curModel,curEnum);
                            continue;
                        }
                        if (curprop.Name == "Discipline")
                        {
                            UnitGroup unitGroup;
                            if (Enum.TryParse(curEnum.ToString(), out unitGroup))
                            {
                                curprop.SetValue(curModel, unitGroup);

                            }
                            continue;
                        }
                        if (curprop.Name == "Name")
                        {
                            curprop.SetValue(curModel, attribute.Description);
                            continue;
                        }
                        var result = attributePropertys.FirstOrDefault(x => x.Name == curprop.Name);
                        if (result != null)
                        {
                            curprop.SetValue(curModel, result.GetValue(attribute));
                        }
                    }
                    return curModel;
                }
            }
            return null;
        }


        /// 根据 Description 的值获取枚举值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="description"></param>
        /// <returns></returns>
        public static T GetEnumByDescription<T>(string description) where T : Enum
        {
            FieldInfo[] fields = typeof(T).GetFields();
            foreach (FieldInfo field in fields)
            {
                object[] attributes = field.GetCustomAttributes(typeof(HelpAttribute), false); //获取描述属性
                if (attributes.Length > 0 && (attributes[0] as HelpAttribute).Description == description)
                {
                    return (T)field.GetValue(null);
                }
            }
            return default(T);
        }


        public static string GetDescriptionByEnum<TEnum>(TEnum curEnum)
        {
            HelpAttribute attribute;
            FieldInfo field = typeof(TEnum).GetField(curEnum.ToString());
            object[] attributes = field.GetCustomAttributes(typeof(HelpAttribute), false);
            if (attributes == null || attributes.Length == 0)
            {
                return null;
            }
            foreach (var item in attributes)
            {
                if (item is HelpAttribute)
                {
                    attribute = item as HelpAttribute;
                    return attribute.Description;
                }
            }
            return null;
        }
    }
}
