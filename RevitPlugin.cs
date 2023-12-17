using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using OfficeOpenXml;
using QiShiLog.Log;
using Revit.Async;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ParameterManager
{
    [Transaction(TransactionMode.Manual)]
    public class RevitPlugin : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            /// 引入下第三方库：防止出现找不到程序集的错误
            var _ = new Microsoft.Xaml.Behaviors.DefaultTriggerAttribute(typeof(Trigger), typeof(Microsoft.Xaml.Behaviors.TriggerBase), null);//解决bug——使用外部应用.addin文件载入插件时，会报错:Could not load file or assembly Microsoft.Xaml.Behaviors（外部命令载入插件不报错） 
            RevitTask.Initialize(commandData.Application);// 作用一：不一定非要采用构造函数引入第三方——视具体情况而定
                                                          // 作用二：初始化RevitTask（需要在Revit的上下文环境中执行）

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;


            /// 开启日志功能
            Logger.Instance.EnableInfoFile = true;


            /// 注册外部事件至缓存中            

            // 获取Revit应用进程——用于手动执行外部事件
            SysCache.Instance.ExternEventExecuteApp = commandData.Application;

            // 注册外部事件至缓存中——获取Revit族库数据至WPF
            LoadFamilyTreeSource loadFamilyTreeSource = new LoadFamilyTreeSource();//获取外部命令            
            ExternalEvent _externalEventLoadFamilyTreeSource = ExternalEvent.Create(loadFamilyTreeSource);//注册外部事件;
            SysCache.Instance.LoadFamilyTreeSourceEventHandler = loadFamilyTreeSource;//存储至缓存类中
            SysCache.Instance.LoadFamilyTreeSourceEvent = _externalEventLoadFamilyTreeSource;

            // 注册外部事件至缓存中——获取UnitGroupToParameterType
            GetUnitGroupToParameterType getUnitGroupToParameterType = new GetUnitGroupToParameterType();//获取外部命令            
            ExternalEvent _externalEventGetUnitGroupToParameterType = ExternalEvent.Create(getUnitGroupToParameterType);//注册外部事件;
            SysCache.Instance.GetUnitGroupToParameterTypeEventHandler = getUnitGroupToParameterType;//存储至缓存类中
            SysCache.Instance.GetUnitGroupToParameterTypeEvent = _externalEventGetUnitGroupToParameterType;

            SysCache.Instance.GetUnitGroupToParameterTypeEventHandler.Execute(SysCache.Instance.ExternEventExecuteApp);
            // 注意：对于ParameterType，不能直接根据中文名称遍历获取——原因：不同的ParameterType，存在中文重名的情况
            //       ——需通过外部事件从revit中获取
            //       ——而不能直接从下面资源层 SysCache.Instance.DiscipToParaTypeToBuiltInGruop中获取



            /// 获取项目字典资源
            //【注意】
            //revit开发中直接输入：@"Assets\DiscipToParaTypeToGroup.txt"——会自动拼接在当前程序集路径后——需手动获取当前程序集所在文件目录
            //（控制台程序才会自动拼接在当前运行程序集后）（Revit UI加载待测试）
            SysCache.Instance.DiscipToParaTypeToBuiltInGruop = Helper.ReadFileForDiscipToParaTypeToGruop(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Assets\DiscipToParaTypeToGroup.txt");
            SysCache.Instance.EditableBuiltInParaGroup = Helper.ReadFileForEditableBuiltInParaGroup(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Assets\EditableBuiltInParameterGroup.txt");


            /// 打开非模态窗口
            Bootstrapper bootstrapper = new Bootstrapper();
            bootstrapper.Run();

            return Result.Succeeded;
        }
    }
}
