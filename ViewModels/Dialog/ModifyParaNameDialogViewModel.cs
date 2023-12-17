using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParameterManager.ViewModels
{
    public class ModifyParaNameDialogViewModel : BindableBase, IDialogAware
    {
        #region IDialogAware接口实现

        public string Title => "编辑参数名";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters) { }

        #endregion

        private string _PrefixText;
        public string PrefixText
        {
            get { return _PrefixText; }
            set { SetProperty(ref _PrefixText, value); }
        }

        private string _SuffixText;
        public string SuffixText
        {
            get { return _SuffixText; }
            set { SetProperty(ref _SuffixText, value); }
        }

        private string _SearchText;
        public string SearchText
        {
            get { return _SearchText; }
            set { SetProperty(ref _SearchText, value); }
        }

        private string _ReplaceText;
        public string ReplaceText
        {
            get { return _ReplaceText; }
            set { SetProperty(ref _ReplaceText, value); }
        }

        public ICommand ModifyCommand
        {
            get => new DelegateCommand(() =>
            {
                DialogParameters dialogParameters = new DialogParameters();
                dialogParameters.Add("参数编辑信息", new string[] { PrefixText, SuffixText, SearchText, ReplaceText});

                DialogResult dialogResult = new DialogResult(ButtonResult.OK, dialogParameters);
                RequestClose?.Invoke(dialogResult);
            });
        }

        public ICommand ClearCommand
        {
            get => new DelegateCommand(() =>
            {
                PrefixText = "";
                SuffixText = "";
                SearchText = "";
                ReplaceText = "";
            });
        }

    }
}
