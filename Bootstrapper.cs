using Prism.Ioc;
using Prism.Regions;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Unity;
using Unity.Lifetime;

namespace ParameterManager
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<Views.MainWindow>();
        }


        protected override IContainerExtension CreateContainerExtension()
        {
            return base.CreateContainerExtension();
        }

        /// <summary>
        /// 用于确认窗口的启动模式：非模态 
        /// </summary>
        protected override void OnInitialized()
        {
            if (Shell is Window window)
            {
                window.Show();
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Region注册View
            containerRegistry.RegisterForNavigation<Views.FamilyParameterContentView>();
            containerRegistry.RegisterForNavigation<Views.OtherParameterContentView>();

            // Dialog注册View
            containerRegistry.RegisterDialog<Views.ModifyParaNameDialogView>();
            containerRegistry.RegisterDialogWindow<Views.DialogWindowBase>("dialogWin");

            containerRegistry.RegisterDialog<Views.ParaCreateDialogView>();
            containerRegistry.RegisterDialogWindow<Views.ParaCreateDialogWindowBase>("dialogWinForParaCreate");

            containerRegistry.RegisterDialog<Views.ParaImportDialogView>();
            containerRegistry.RegisterDialogWindow<Views.ParaImportDialogWindowBase>("dialogWinForParaImport");
        }
    }
}
