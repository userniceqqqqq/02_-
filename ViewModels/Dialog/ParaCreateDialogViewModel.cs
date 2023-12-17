using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParameterManager.ViewModels
{
    class ParaCreateDialogViewModel : BindableBase, IDialogAware
    {
        #region IDialogAware接口实现
        public string Title => "参数创建";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog() { return true; }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            List<ParaCreateDataGridModel> createdParas = parameters.GetValue<List<ParaCreateDataGridModel>>("参数创建情况");
            int successCount = 0;
            createdParas.ForEach(item =>
            {
                if (item.IsCreatedSuccess)
                {
                    successCount++;
                }
                CreatedParaModels.Add(item);
            });
            ParaCreatedDescription = $"{successCount}个参数被成功创建，{createdParas.Count - successCount}个参数创建失败";
        }

        #endregion


        public ObservableCollection<ParaCreateDataGridModel> CreatedParaModels { get; set; } = new ObservableCollection<ParaCreateDataGridModel>();

        private string _ParaCreatedDescription;
        public string ParaCreatedDescription
        {
            get { return _ParaCreatedDescription; }
            set { SetProperty(ref _ParaCreatedDescription, value); }
        }

        public ICommand CloseCommand
        {
            get => new DelegateCommand(() =>
            {               
                IDialogResult dialogResult = new Prism.Services.Dialogs.DialogResult(ButtonResult.OK);
                RequestClose?.Invoke(dialogResult);
            });
        }
    }
}
