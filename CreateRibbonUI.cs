﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ParameterManager
{

    [Transaction(TransactionMode.Manual)]
    public class CreateRibbonUI : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            RibbonPanel rp;
            try
            {
                //【1】创建一个RibbonTab~(无法通过new创建)  命名：XTool
                application.CreateRibbonTab("XTool");
                //【2】在刚RibbonTab中创建一个RibbonPanel  命名：Manager
                rp = application.CreateRibbonPanel("XTool", "Manager");
            }
            catch
            {
                //【2】获取RibbonTab中已创建的RibbonPanel
                rp = application.GetRibbonPanels("XTool").FirstOrDefault();
            }           

            //【3】指定程序集文件（名称、路径），以及使用的类名称
            string assemblyPath = Assembly.GetExecutingAssembly().Location;//推荐：当程序集与.dll路径发生变化时，仅需要修改.addin文件里的路径即可           
            string classNameParameterManagerDemo = "ParameterManager.RevitPlugin"; //获取被调用的类名称（程序集名.类名）

            //【4】创建PushButton按钮           
            PushButtonData pbdTwo = new PushButtonData("ParameterManagerForRevit", "参数管理", assemblyPath, classNameParameterManagerDemo);//参数1：在程序内部储存的名称，必须唯一；参数2：在按钮显示的名称；参数3：程序集的路径；程序集：被调用的类名称
            PushButton pushButtonTwo = rp.AddItem(pbdTwo) as PushButton;   //将按钮添加到RibbonPanel面板中
            pushButtonTwo.LargeImage = new BitmapImage(new Uri("pack://application:,,,/ParameterManager;component/Assets/参数管理_橙32.png", UriKind.Absolute));   //添加图片
            pushButtonTwo.ToolTip = "批量创建、导入或导出Revit中的各种参数";   //添加默认提示信息

            return Result.Succeeded;
        }
    }
}
