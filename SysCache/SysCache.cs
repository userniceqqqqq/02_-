using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ParameterManager.Events;
using ParameterManager.Views;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ParameterManager
{
    class SysCache
    {
        /// <summary>
        /// 单例模式
        /// </summary>
        private SysCache() { }

        private static SysCache _instance;
        public static SysCache Instance
        {
            get
            {
                if (ReferenceEquals(null, _instance))
                {
                    _instance = new SysCache();
                }
                return _instance;
            }
        }

        public UIApplication ExternEventExecuteApp { get; set; }//用于触发外部事件


        public LoadFamilyTreeSource LoadFamilyTreeSourceEventHandler { get; set; }
        public ExternalEvent LoadFamilyTreeSourceEvent { get; set; }  //建立外部事件       
        public IList<Element> FamilyTreeSourceList { get; set; } = new List<Element>();
        public Family CurFamily { get; set; } = null;

        //缓存资源层内容
        public Dictionary<string, Dictionary<string, string>> DiscipToParaTypeToBuiltInGruop { get; set; }
        public List<BuiltInParameterGroup> EditableBuiltInParaGroup { get; set; }



        public GetUnitGroupToParameterType GetUnitGroupToParameterTypeEventHandler { get; set; }
        public ExternalEvent GetUnitGroupToParameterTypeEvent { get; set; }  //建立外部事件 
        public Dictionary<UnitGroup, List<ParameterType>> UnitGroupToParameterType { get; set; }

        public ExternalEvent LoadEvent { get; set; }  //建立外部事件  Dictionary<string, Dictionary<string, string>> ugToParaToGroup 


    }
}
