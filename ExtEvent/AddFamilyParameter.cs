using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public class AddFamilyParameter : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;
            //UI级应用程序表示的数据库级应用程序——提供对文档、选项的访问
            Application serviceApp = app.Application;

            Family family = new FilteredElementCollector(doc).OfClass(typeof(Family)).FirstOrDefault(x => x.Name == "xxxfamilyName") as Family;//根据族名字过滤获取项目中的族
            if (family == null)
            {
                return;
            }
            Document familyDoc = doc.EditFamily(family);
            if (familyDoc == null)
            {
                return;
            }
            FamilyManager familyManager = familyDoc.FamilyManager;

            /// 创建共享族参数 
            // 【1】基本信息——参数名、分组名、参数类型
            string curPath = Assembly.GetExecutingAssembly().Location;
            string curFolder = Path.GetDirectoryName(curPath);
            string shareParameterFilePath = curFolder + "\\ShareParameter.txt";
            if (!File.Exists(shareParameterFilePath))
            {
                FileStream fileStream = File.Create(shareParameterFilePath);
                fileStream.Close();
            }
            string shareParameterGroupName = "xxxGroupName";
            string shareParameterName = "xxxParameterName";
            ParameterType shareparameterType = ParameterType.Length;

            //【2】打开共享族参数定义文件
            serviceApp.SharedParametersFilename = shareParameterFilePath;
            DefinitionFile shareDefinitionFile = serviceApp.OpenSharedParameterFile();
            if (shareDefinitionFile == null)
            {
                return;
            }
            //【3】查找or创建共享族参数分组
            DefinitionGroup shareGroup = shareDefinitionFile.Groups.get_Item(shareParameterGroupName);
            if (shareGroup==null)
            {
                shareGroup = shareDefinitionFile.Groups.Create("xxxGroupName");
            }

            //【4】查找or创建共享族参数的定义
            ExternalDefinition shareParameterDef = shareGroup.Definitions.get_Item(shareParameterName) as ExternalDefinition;

            //【附加——共享族参数的附加属性：HideWhenNoValue、UserModifiableGUID、Visible、Description~通过ExternalDefinitionCreationOptions设置】
            if (shareParameterDef==null)
            {
                shareParameterDef = shareGroup.Definitions.Create(new ExternalDefinitionCreationOptions(shareParameterName, shareparameterType)) as ExternalDefinition;
            }

            //【5】创建共享族参数
            FamilyParameter newShareParameter = familyManager.AddParameter(shareParameterDef,BuiltInParameterGroup.INVALID,true);

            //【扩展：规程决定了参数类型UnitGroup——>ParameterType——>BuiltInParameterGroup】            
            UnitType unitType = newShareParameter.Definition.UnitType;
            UnitGroup discipline =UnitUtils.GetUnitGroup(unitType);
            Enum.GetValues(typeof(UnitGroup));
            // 注意区分：
            // 1、ViewDiscipline——视图规程
            // 扩展：子规程：View——Parameters——parameters.Definition.Name="子规程"        
            // 2、ProductType——产品专业
            // Autodesk.Revit.ApplicationServices.ControlledApplication.Product;

            familyManager.SetDescription(newShareParameter,"xxxdescription");


            //GlobalParameter
            //ParameterMap_映射
            //ParameterSet_集合    


        }

        public string GetName()
        {
            return "AddFamilyParameter";
        }
    }
}
