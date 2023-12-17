using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using QiShiLog;
using QiShiLog.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ParameterManager.ViewModels
{
    class ParaImportDialogViewModel : BindableBase, IDialogAware
    {
        #region IDialogAware接口实现
        public string Title => "参数创建";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog() { return true; }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            List<ParaImportDataGridModel> importParas = parameters.GetValue<List<ParaImportDataGridModel>>("待导入参数");
            if (importParas[0].GroupName == "共享参数" || importParas[0].GroupName == "族参数")
            {
                ParaGroupModels.Add(new CheckAndComboModel()
                {
                    Name= "共享参数/族参数",
                    IsSelect = true
                });
            }
            else
            {
                ParaGroupModels.Add(new CheckAndComboModel()
                {
                    Name = "共享参数组",
                    IsSelect = true
                });
            }
            importParas.ForEach(item =>
            {
                var result = ParaGroupModels.FirstOrDefault(x => x.Name == item.GroupName);
                if (result==null)
                {
                    ParaGroupModels.Add(new CheckAndComboModel()
                    {
                        Name = item.GroupName,
                        IsCheck = true
                    });
                }
                ImportParaModels.Add(item);
            });
            int displayCount = ParaGroupModels.Count - 1;
            ComboBoxMaxDropDownHeight = displayCount * 17.5;
        }

        #endregion



        public ObservableCollection<ParaImportDataGridModel> ImportParaModels { get; set; } = new ObservableCollection<ParaImportDataGridModel>();

        private string _SearchText = "";
        public string SearchText
        {
            get { return _SearchText; }
            set { SetProperty(ref _SearchText, value, SrerchTextChanged); }
        }
        public void SrerchTextChanged()
        {
            int importCount = 0;
            if (SearchText == "" || SearchText == null)
            {
                ImportParaModels.ToList().ForEach(item =>
                {
                    item.NodeVisibility = Visibility.Visible;
                    if (item.IsCheck == true)
                    {
                        importCount++;
                    }
                });
                ParaImportDescription = $"共选中{importCount}个参数";
                return;
            }
            ImportParaModels.ToList().ForEach(item =>
            {
                if (item.ParaName.Contains(SearchText.Trim()))
                {
                    item.NodeVisibility = Visibility.Visible;
                    if (item.IsCheck == true)
                    {
                        importCount++;
                    }
                }
                else
                {
                    item.NodeVisibility = Visibility.Collapsed;
                }
            });
            ParaImportDescription = $"共选中{importCount}个参数";
        }


        private bool? _DataGridAllIsCheck = false;
        public bool? DataGridAllIsCheck //null表示未全选该节点的所有子节点
        {
            get { return _DataGridAllIsCheck; }
            set { SetProperty(ref _DataGridAllIsCheck, value); }
        }
        public ICommand AllItemCheckChangedCommand
        {
            get => new DelegateCommand(() =>
            {
                int importCount = 0;
                foreach (var item in ImportParaModels)
                {
                    item.IsCheck = DataGridAllIsCheck;
                    if (item.IsCheck==true && item.NodeVisibility==Visibility.Visible)
                    {
                        importCount++;
                    }
                }
                ParaImportDescription= $"共选中{importCount}个参数";
            });
        }

        public ICommand ItemCheckChangedCommand
        {
            get => new DelegateCommand<Object>((obj) =>
            {
                int importCount = 0;
                ParaImportDataGridModel curItem = obj as ParaImportDataGridModel;
                DataGridAllIsCheck = curItem.IsCheck;
                foreach (var item in ImportParaModels)
                {
                    if (item.IsSelect == true)
                    {
                        item.IsCheck = curItem.IsCheck;
                    }
                    if (item.IsCheck == true && item.NodeVisibility == Visibility.Visible)
                    {
                        importCount++;
                    }
                    if (item.IsCheck != curItem.IsCheck && DataGridAllIsCheck != null)
                    {
                        DataGridAllIsCheck = null;
                    }
                }
                ParaImportDescription = $"共选中{importCount}个参数";
            });
        }


        public ObservableCollection<CheckAndComboModel> ParaGroupModels { get; set; } = new ObservableCollection<CheckAndComboModel>();

        private double _ComboBoxMaxDropDownHeight;
        public double ComboBoxMaxDropDownHeight
        {
            get { return _ComboBoxMaxDropDownHeight; }
            set { SetProperty(ref _ComboBoxMaxDropDownHeight, value); }
        }

        public ICommand DisplayParaGroupCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                try
                {
                    CheckAndComboModel curAttribute = obj as CheckAndComboModel;
                    bool curValue = (bool)curAttribute.IsCheck;
                    var result = ParaGroupModels.FirstOrDefault(x => x.IsCheck != curValue && x != ParaGroupModels[0]);

                    ParaGroupModels[0].IsSelect = true;
                    for (int i = 1; i < ParaGroupModels.Count; i++)
                    {
                        ParaGroupModels[i].IsSelect = false;
                    }

                    if ( result == null)//此时为全选或全不选
                    {                        
                        //ParaGroupModels[0].IsCheck = curValue;                        
                        if (curValue)//全选
                        {
                            foreach (var item in ImportParaModels)
                            {
                                if (item.NodeVisibility != Visibility.Visible && item.ParaName.Contains(SearchText))
                                {
                                    item.NodeVisibility = Visibility.Visible;
                                }
                            }
                        }
                        else//全不选
                        {
                            foreach (var item in ImportParaModels)
                            {
                                if (item.NodeVisibility == Visibility.Visible)
                                {
                                    item.NodeVisibility = Visibility.Collapsed;
                                }
                            }
                        }
                    }
                    else//非全选
                    {
                        //ParaGroupModels[0].IsCheck = null;                       
                        foreach (var item in ImportParaModels)
                        {
                            var paraDiaplayResult = ParaGroupModels.FirstOrDefault(x=>x.IsCheck==true && x.Name == item.GroupName);
                            if (paraDiaplayResult !=null && item.ParaName.Contains(SearchText))
                            {
                                item.NodeVisibility = Visibility.Visible;
                                continue;
                            }
                            item.NodeVisibility = Visibility.Collapsed;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }

            });
        }


        private string _ParaImportDescription;
        public string ParaImportDescription
        {
            get { return _ParaImportDescription; }
            set { SetProperty(ref _ParaImportDescription, value); }
        }

        public ICommand ImportCommand
        {
            get => new DelegateCommand(() =>
            {
                List<ParaImportDataGridModel> tempParas = new List<ParaImportDataGridModel>();
                ImportParaModels.ToList().ForEach(item=> 
                {
                    if (item.IsCheck == true && item.NodeVisibility == Visibility.Visible)
                    {
                        tempParas.Add(item);
                    }
                });
                if (tempParas.Count==0)
                {
                    MessageBox.Show("请至少选择一个将要导入的参数", "提示");
                    return;
                }
                DialogParameters dialogParameters = new DialogParameters();
                dialogParameters.Add("需导入参数",tempParas);
                IDialogResult dialogResult = new Prism.Services.Dialogs.DialogResult(ButtonResult.OK, dialogParameters);
                RequestClose?.Invoke(dialogResult);
            });
        }
    }
}
