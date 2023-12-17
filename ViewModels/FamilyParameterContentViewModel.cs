using Autodesk.Revit.DB;
using ParameterManager.Events;
using ParameterManager.Views;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using QiShiLog;
using QiShiLog.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Unity;
using Visibility = System.Windows.Visibility;
using Binding = System.Windows.Data.Binding;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Data;
using Microsoft.Xaml.Behaviors;
using Prism.Services.Dialogs;
using Revit.Async;
using Autodesk.Revit.ApplicationServices;
using System.Windows;
using Application = Autodesk.Revit.ApplicationServices.Application;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Border = System.Windows.Controls.Border;
using System.Threading;
using Autodesk.Revit.UI;
using ComboBox = System.Windows.Controls.ComboBox;
using Autodesk.Revit.UI.Selection;

namespace ParameterManager.ViewModels
{
    class FamilyParameterContentViewModel : BindableBase
    {
        public FamilyParameterContentViewModel()
        {
            InitComboBox();
            SysCache.Instance.LoadFamilyTreeSourceEventHandler.Execute(SysCache.Instance.ExternEventExecuteApp);
            InitTree();
        }

        private string _Title = "族参数";
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }


        #region DataGrid编辑交互 

        public ObservableCollection<ComboModelBase> AddParameterModels { get; set; } = new ObservableCollection<ComboModelBase>();
        public ObservableCollection<ComboModelBase> ImportOrExportModels { get; set; } = new ObservableCollection<ComboModelBase>();
        public ObservableCollection<ComboModelBase> BatchActionModels { get; set; } = new ObservableCollection<ComboModelBase>();
        public ObservableCollection<CheckAndComboModel> AdditionPropertyModels { get; set; } = new ObservableCollection<CheckAndComboModel>();
        public ObservableCollection<ComboModelBase> DocSourceModels { get; set; } = new ObservableCollection<ComboModelBase>();

        public ICommand AddParameterCommand
        {
            get => new DelegateCommand<RoutedEventArgs>(async (args) =>
            {
                try
                {
                    if (!(args.OriginalSource is Border))
                    {
                        return;
                    }
                    Border curBorder = args.OriginalSource as Border;
                    if (curBorder.Name != "templateRoot")
                    {
                        return;
                    }
                    ComboBox comboBox = args.Source as ComboBox;
                    if (comboBox.SelectedIndex == 0)
                    {
                        int curCount = 0;
                        string curName;
                        ParaDataGridModel result;
                        do
                        {
                            curName = $"参数-{++curCount}";
                            result = DatagridModels.FirstOrDefault(x => x.Name == curName);
                        }
                        while (result != null);
                        DatagridModels.Add(new ParaDataGridModel()
                        {
                            Name = curName
                        });
                    }
                    else if (comboBox.SelectedIndex == 1)
                    {
                        await RevitTask.RunAsync((uiApp) =>
                        {
                            DatagridModels.ToList().ForEach(x =>
                            {
                                if (x.IsSelect == true)
                                {
                                    x.IsSelect = false;
                                }
                            });
                            //选择族—— Reference类：对该构件(element)或者基本几何（line,face）的指代，一个载体
                            UIDocument uidoc = uiApp.ActiveUIDocument;
                            Document doc = uidoc.Document;
                            Reference reference = null;
                            try
                            {
                                reference = uidoc.Selection.PickObject(ObjectType.Element, new LoadFamilySelectionFilter(), "请选择一个载入族"); //参数：指定类型，过滤器,提示信息
                            }
                            catch
                            {
                                args.Handled = true;
                                return;
                            }

                            Element ele = doc.GetElement(reference);
                            //ParameterSet parameters = ele.Parameters;                        
                            Family curFamily = (ele as FamilyInstance).Symbol.Family;
                            Document familyDoc = doc.EditFamily(curFamily);
                            FamilyParameterSet familyParameterSet = familyDoc.FamilyManager.Parameters;
                            int count = 0;
                            foreach (FamilyParameter item in familyParameterSet)
                            {
                                InternalDefinition definition = item.Definition as InternalDefinition;
                                if (!(definition.BuiltInParameter == BuiltInParameter.INVALID)) // 过滤掉内建参数
                                {
                                    continue;
                                }
                                UnitGroup discipline = default;
                                foreach (UnitGroup unitGroup in Enum.GetValues(typeof(UnitGroup)))
                                {
                                    bool value = SysCache.Instance.UnitGroupToParameterType[unitGroup].Contains(definition.ParameterType);
                                    if (value) { discipline = unitGroup; }
                                }
                                DatagridModels.Add(new ParaDataGridModel()
                                {
                                    Name = definition.Name,
                                    IsShareParameter = item.IsShared,
                                    ParaKind = item.IsShared ? ParaKindEnum.SharePara : ParaKindEnum.FamilyPara,
                                    Discipline = discipline,
                                    ParaType = definition.ParameterType,
                                    ParaGroup = definition.ParameterGroup,
                                    IsInstancePara = item.IsInstance,
                                    IsSelect = true
                                });
                                count++;
                            }
                            MessageBox.Show($"共导入{count}个参数", "提示");
                        });
                    }
                    if (DataGridAllIsCheck == true)
                    {
                        DataGridAllIsCheck = null;
                    }
                    args.Handled = true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }               
            });
        }

        public ICommand ImportOrExportCommand
        {
            get => new DelegateCommand<ComboBox>(async(curComboBox) =>
            {
                string ItenName = ((curComboBox.SelectedItem) as ComboModelBase).Name;
                curComboBox.SelectedIndex = 0;
                try
                {
                    switch (ItenName)
                    {
                        case "导入Excel":
                            ImportExcel();
                            break;
                        case "导出Excel":
                            ExportExcel();
                            break;
                        case "导入共享参数":
                            await ImportShareParaAsync();
                            break;
                        case "导出共享参数":
                            await ExportShareParaAsync();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }
            });
        }

        #region 辅助方法——参数导入导出

        private List<ParaImportDataGridModel> paraImportModels = new List<ParaImportDataGridModel>();
        private void ImportExcel()
        {
            try
            {
                paraImportModels.Clear();
                List<ParaImportDataGridModel> tempParaImportModels = new List<ParaImportDataGridModel>();
                string filePath;
                int columnOfParaName;
                Dictionary<string, int> indexOfColumnPropertyDict = new Dictionary<string, int>();
                Dictionary<string, int> indexOfRowObjectDict = new Dictionary<string, int>();

                //【1】找到对应Excel
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = @"C:\";
                openFileDialog.Filter = "Excel文件|*.xlsx";
                if (openFileDialog.ShowDialog() == true)
                {
                    filePath = openFileDialog.FileName;
                }
                else
                {
                    return;
                }

                //【2】读取Excel文件
                FileInfo file = new FileInfo(filePath);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (ExcelPackage excelPackage = new ExcelPackage(file))
                {
                    //1、得到指定的Sheet
                    //ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets["Parameter List"];
                    ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets[0];

                    //2、获取列与参数属性的映射关系
                    for (int i = 1; i < excelWorksheet.Dimension.Columns + 1; i++) //originalExcelWorksheet.Dimension.Rows/Columns获取Worksheet的行/列数（不可靠，可能包含空格行）
                    {
                        if (string.IsNullOrEmpty(Convert.ToString(excelWorksheet.Cells[1, i].Value))) //string.IsNullOrEmpty()判断该单元格是否为null，为null返回True
                        {
                            continue;
                        }
                        indexOfColumnPropertyDict.Add(excelWorksheet.Cells[1, i].Value.ToString(), i);
                    }

                    //3、获取ParaModel
                    columnOfParaName = indexOfColumnPropertyDict["参数名"];
                    for (int j = 2; j < excelWorksheet.Dimension.Rows + 1; j++)
                    {
                        if (string.IsNullOrEmpty(Convert.ToString(excelWorksheet.Cells[j, columnOfParaName].Value))) //string.IsNullOrEmpty()判断该单元格是否为null，为null返回True
                        {
                            continue;
                        }
                        indexOfRowObjectDict.Add(excelWorksheet.Cells[j, columnOfParaName].Value.ToString(), j);
                        tempParaImportModels.Add(new ParaImportDataGridModel()
                        {
                            ParaName = excelWorksheet.Cells[j, columnOfParaName].Value.ToString(),
                            GroupName = excelWorksheet.Cells[j, indexOfColumnPropertyDict["共享参数/族参数"]].Value.ToString()
                        });
                    }
                }

                //3、导入ParaModel
                if (tempParaImportModels.Count == 0)
                {
                    MessageBox.Show("该文件中不存在参数信息", "提示");
                    return;
                }

                DialogParameters dialogParameters = new DialogParameters();
                dialogParameters.Add("待导入参数", tempParaImportModels);
                dialogService.ShowDialog("ParaImportDialogView", dialogParameters, DoDialogForParaImportResult, "dialogWin");

                if (paraImportModels.Count == 0)
                {
                    return;
                }
                using (ExcelPackage excelPackage = new ExcelPackage(file))
                {
                    ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets[0];
                    foreach (ParaImportDataGridModel item in paraImportModels)
                    {
                        int row = indexOfRowObjectDict[item.ParaName];

                        ParaDataGridModel paraDataGridModel = new ParaDataGridModel()
                        {
                            Name = item.ParaName,
                            ParaKind = HelpEnum.GetEnumByDescription<ParaKindEnum>(item.GroupName),
                            ParaValue = excelWorksheet.Cells[row, indexOfColumnPropertyDict["参数值"]].Value.ToString(),
                            ParaFormula = excelWorksheet.Cells[row, indexOfColumnPropertyDict["参数公式"]].Value.ToString(),
                            Description = excelWorksheet.Cells[row, indexOfColumnPropertyDict["参数说明"]].Value.ToString()
                        };

                        var resultDiscipline = DisciplineModels.FirstOrDefault(x => x.Name == excelWorksheet.Cells[row, indexOfColumnPropertyDict["规程"]].Value.ToString());
                        if (resultDiscipline != null)
                        {
                            paraDataGridModel.Discipline = resultDiscipline.Discipline;
                        }

                        var resultParaType = paraDataGridModel.ParaTypeModels.FirstOrDefault(x => x.Name == excelWorksheet.Cells[row, indexOfColumnPropertyDict["参数类型"]].Value.ToString());
                        if (resultParaType != null)
                        {
                            paraDataGridModel.ParaType = resultParaType.ParaType;
                        }

                        var resultParaGroup = ParaGroupModels.FirstOrDefault(x => x.Name == excelWorksheet.Cells[row, indexOfColumnPropertyDict["参数分组"]].Value.ToString());
                        if (resultParaGroup != null)
                        {
                            paraDataGridModel.ParaGroup = resultParaGroup.ParaGroup;
                        }

                        var resultIsInstance = InstanceOrTypeModels.FirstOrDefault(x => x.Name == excelWorksheet.Cells[row, indexOfColumnPropertyDict["实例/类型"]].Value.ToString());
                        if (resultIsInstance != null)
                        {
                            paraDataGridModel.IsInstancePara = (bool)resultIsInstance.BoolValue;
                        }

                        if (paraDataGridModel.ParaKind == ParaKindEnum.SharePara)
                        {
                            paraDataGridModel.IsShareParameter = true;

                            string valueModifiable = Convert.ToString(excelWorksheet.Cells[row, indexOfColumnPropertyDict["用户可编辑"]].Value);
                            if (!String.IsNullOrEmpty(valueModifiable))
                            {
                                var resultModifiable = IsModifiableModels.FirstOrDefault(x => x.Name == valueModifiable);
                                if (resultModifiable != null)
                                {
                                    paraDataGridModel.UserModifiable = (bool)resultModifiable.BoolValue;
                                }
                            }

                            string valueVisibile = Convert.ToString(excelWorksheet.Cells[row, indexOfColumnPropertyDict["可见性"]].Value);
                            if (!string.IsNullOrEmpty(valueVisibile))
                            {
                                var resultVisibile = ParaVisibleModels.FirstOrDefault(x => x.Name == valueVisibile);
                                if (resultVisibile != null)
                                {
                                    paraDataGridModel.ParaVisibility = (bool)resultVisibile.BoolValue;
                                }
                            }

                            string valueIsHide = Convert.ToString(excelWorksheet.Cells[row, indexOfColumnPropertyDict["无值时隐藏"]].Value);
                            if (!string.IsNullOrEmpty(valueIsHide))
                            {
                                var resultIsHide = IsHideModels.FirstOrDefault(x => x.Name == valueIsHide);
                                if (resultIsHide != null)
                                {
                                    paraDataGridModel.HideWhenNoValue = (bool)resultIsHide.BoolValue;
                                }
                            }
                        }
                        DatagridModels.Add(paraDataGridModel);
                    }
                }
                if (DataGridAllIsCheck == true)
                {
                    DataGridAllIsCheck = null;
                }
                MessageBox.Show($"共导入{paraImportModels.Count}个参数", "提示");
            }
            catch (Exception ex)
            {
                throw new Exception("封装的异常", ex);
            }

        }
        private void DoDialogForParaImportResult(IDialogResult dialogResult)
        {
            if (dialogResult.Result == ButtonResult.OK)
            {
                paraImportModels = dialogResult.Parameters.GetValue<List<ParaImportDataGridModel>>("需导入参数");
            }
        }

        private void ExportExcel()
        {
            try
            {
                var result = DatagridModels.FirstOrDefault(x => x.IsCheck == true && x.NodeVisibility == Visibility.Visible);
                if (result == null)
                {
                    MessageBox.Show("请勾选至少一个参数", "提示");
                    return;
                }

                int count = 0;
                string filePath;
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.InitialDirectory = @"C:\";
                saveFileDialog.Filter = "Excel文件|*.xlsx";
                if (saveFileDialog.ShowDialog() == true)
                {
                    filePath = saveFileDialog.FileName; //会自动加后缀扩展名
                }
                else
                {
                    return;
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                FileInfo file = new FileInfo(filePath);
                using (ExcelPackage excelPackage = new ExcelPackage(file))
                {
                    //创建Worksheet表                    
                    ExcelWorksheet saveExcelWorkSheet = excelPackage.Workbook.Worksheets.Add("Parameter List");

                    //设置基本样式
                    saveExcelWorkSheet.Cells.Style.Font.Name = "宋体";
                    saveExcelWorkSheet.Cells.Style.Font.Size = 12F;
                    saveExcelWorkSheet.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    saveExcelWorkSheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    //设置指定单元格内容
                    List<string> ColumnPropertys = new List<string>() { "参数名", "共享参数/族参数", "规程", "参数类型", "参数分组", "实例/类型", "参数值", "参数公式", "参数说明", "用户可编辑", "可见性", "无值时隐藏" };
                    foreach (string item in ColumnPropertys)
                    {
                        int columnIndex = ColumnPropertys.IndexOf(item) + 1;
                        saveExcelWorkSheet.Cells[1, columnIndex].Value = item;
                    }
                    int rowIndex = 2;
                    foreach (ParaDataGridModel item in DatagridModels)
                    {
                        if (item.IsCheck != true || item.NodeVisibility != Visibility.Visible)
                        {
                            continue;
                        }
                        saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("参数名") + 1].Value = item.Name;
                        saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("参数值") + 1].Value = item.ParaValue;
                        saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("参数公式") + 1].Value = item.ParaFormula;
                        saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("参数说明") + 1].Value = item.Description;
                        saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("共享参数/族参数") + 1].Value = HelpEnum.GetDescriptionByEnum(item.ParaKind);

                        var resultDiscipline = DisciplineModels.FirstOrDefault(x => x.Discipline == item.Discipline);
                        saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("规程") + 1].Value = resultDiscipline.Name;

                        var resultParaType = item.ParaTypeModels.FirstOrDefault(x => x.ParaType == item.ParaType);
                        saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("参数类型") + 1].Value = resultParaType.Name;

                        var resultParaGroup = ParaGroupModels.FirstOrDefault(x => x.ParaGroup == item.ParaGroup);
                        saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("参数分组") + 1].Value = resultParaGroup.Name;

                        var resultIsInstance = InstanceOrTypeModels.FirstOrDefault(x => x.BoolValue == item.IsInstancePara);
                        saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("实例/类型") + 1].Value = resultIsInstance.Name;

                        if (item.ParaKind == ParaKindEnum.SharePara)
                        {
                            if (item.UserModifiable != null)
                            {
                                var resultModifiable = IsModifiableModels.FirstOrDefault(x => x.BoolValue == item.UserModifiable);
                                saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("用户可编辑") + 1].Value = resultModifiable.Name;
                            }

                            if (item.ParaVisibility != null)
                            {
                                var resultVisibile = ParaVisibleModels.FirstOrDefault(x => x.BoolValue == item.ParaVisibility);
                                saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("可见性") + 1].Value = resultVisibile.Name;
                            }

                            if (item.HideWhenNoValue != null)
                            {
                                var resultIsHide = IsHideModels.FirstOrDefault(x => x.BoolValue == item.HideWhenNoValue);
                                saveExcelWorkSheet.Cells[rowIndex, ColumnPropertys.IndexOf("无值时隐藏") + 1].Value = resultIsHide.Name;
                            }
                        }
                        rowIndex++;
                        count++;
                    }
                    rowIndex--;
                    //设置行高、列宽
                    //saveExcelWorkSheet.Cells.Style.ShrinkToFit = true;//不改变单元格大小，内容自动适应单元格大小
                    for (int c = 1; c <= saveExcelWorkSheet.Dimension.Columns; c++)
                    {
                        saveExcelWorkSheet.Column(c).Width = 24;
                    }
                    for (int c = 1; c <= saveExcelWorkSheet.Dimension.Rows; c++)
                    {
                        saveExcelWorkSheet.Row(c).Height = 18;
                    }
                    //saveExcelWorkSheet.Row(1).CustomHeight = true;//自动调整行高

                    //设置区域背景颜色（设置背景颜色之前一定要设置PatternType）
                    // 首行
                    saveExcelWorkSheet.Cells["A1:L1"].Style.Font.Bold = true;
                    saveExcelWorkSheet.Cells["A1:L1"].Style.Font.Color.SetColor(System.Drawing.Color.Black);                                      
                    saveExcelWorkSheet.Cells["A1:L1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    saveExcelWorkSheet.Cells["A1:L1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 217, 102));
                    // 其余行
                    saveExcelWorkSheet.Cells[$"A2:L{rowIndex}"].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(64, 64, 64));
                    // 首列
                    saveExcelWorkSheet.Cells[$"A2:A{rowIndex}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    saveExcelWorkSheet.Cells[$"A2:A{rowIndex}"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 203, 173));

                    //设置单元格区域的边框
                    //首列
                    for (int i = 1; i <= 12; i++)
                    {
                        saveExcelWorkSheet.Cells[1, i].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.Black);
                    }
                    // 外边框
                    saveExcelWorkSheet.Cells[$"A1:L{rowIndex}"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium, System.Drawing.Color.Black);

                    excelPackage.SaveAs(file);

                    MessageBox.Show($"共导出{count}个参数", "提示");
                    Process.Start(filePath);
                    Process.Start(Path.GetDirectoryName(filePath));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("封装的异常", ex);
            }
        }

        private async Task ImportShareParaAsync()
        {
            try
            {
                paraImportModels.Clear();
                List<ParaImportDataGridModel> tempParaImportModels = new List<ParaImportDataGridModel>();
                string filePath;
                // 1】获取共享参数文件
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = @"C:\";
                openFileDialog.Filter = "文本文件|*.txt";
                if (openFileDialog.ShowDialog() == true)
                {
                    filePath = openFileDialog.FileName;
                }
                else
                {
                    return;
                }

                await RevitTask.RunAsync((uiApp) =>
                {
                    // 2】打开共享族参数定义文件
                    Application serviceApp = uiApp.Application;
                    try
                    {
                        serviceApp.SharedParametersFilename = filePath;
                    }
                    catch
                    {
                        MessageBox.Show($"您选择的文件格式有误，请选择格式正确的共享参数文件", "提示");
                        return;
                    }

                    DefinitionFile shareDefinitionFile = serviceApp.OpenSharedParameterFile();
                    DefinitionGroups definitionGroups = shareDefinitionFile.Groups;

                    // 3】获取参数组内参数定义
                    foreach (DefinitionGroup group in definitionGroups)
                    {
                        string groupName = group.Name;
                        foreach (Definition definition in group.Definitions)
                        {
                            tempParaImportModels.Add(new ParaImportDataGridModel()
                            {
                                ParaName = definition.Name,
                                GroupName = groupName
                            });
                        }
                    }
                    if (tempParaImportModels.Count == 0)
                    {
                        MessageBox.Show("该文件中不存在共享参数信息", "提示");
                        return;
                    }
                    // 4】选择需导入的共享参数
                    DialogParameters dialogParameters = new DialogParameters();
                    dialogParameters.Add("待导入参数", tempParaImportModels);
                    dialogService.ShowDialog("ParaImportDialogView", dialogParameters, DoDialogForParaImportResult, "dialogWin");
                    if (paraImportModels.Count == 0)
                    {
                        return;
                    }

                    //5】导入选中的共享参数
                    foreach (ParaImportDataGridModel item in paraImportModels)
                    {
                        ParaDataGridModel paraDataGridModel = new ParaDataGridModel()
                        {
                            Name = item.ParaName,
                            ParaKind = ParaKindEnum.SharePara,
                            IsShareParameter = true
                        };

                        DefinitionGroup shareGroup = shareDefinitionFile.Groups.get_Item(item.GroupName);
                        ExternalDefinition shareParameterDef = shareGroup.Definitions.get_Item(item.ParaName) as ExternalDefinition;

                        paraDataGridModel.UserModifiable = shareParameterDef.UserModifiable;
                        paraDataGridModel.HideWhenNoValue = shareParameterDef.HideWhenNoValue;
                        paraDataGridModel.ParaVisibility = shareParameterDef.Visible;
                        if (shareParameterDef.Description == null)
                        {
                            paraDataGridModel.Description = "";
                        }
                        else
                        {
                            paraDataGridModel.Description = shareParameterDef.Description;
                        }
                        paraDataGridModel.ParaType = shareParameterDef.ParameterType;

                        bool isLoop = true;
                        foreach (KeyValuePair<UnitGroup, List<ParameterType>> ug in SysCache.Instance.UnitGroupToParameterType)
                        {
                            foreach (var curItem in ug.Value)
                            {
                                if (curItem == shareParameterDef.ParameterType)
                                {
                                    paraDataGridModel.Discipline = ug.Key;
                                    isLoop = false;
                                    break;
                                }
                            }
                            if (!isLoop)
                            {
                                break;
                            }
                        }

                        string curBuiltInGruop = SysCache.Instance.DiscipToParaTypeToBuiltInGruop[paraDataGridModel.Discipline.ToString()][LabelUtils.GetLabelFor(paraDataGridModel.ParaType)];
                        foreach (BuiltInParameterGroup itemforGroup in SysCache.Instance.EditableBuiltInParaGroup)
                        {
                            if (LabelUtils.GetLabelFor(itemforGroup) == curBuiltInGruop)
                            {
                                paraDataGridModel.ParaGroup = itemforGroup;
                                break;
                            }
                        }

                        DatagridModels.Add(paraDataGridModel);
                    }
                    if (DataGridAllIsCheck == true)
                    {
                        DataGridAllIsCheck = null;
                    }
                    MessageBox.Show($"共导入{paraImportModels.Count}个参数", "提示");
                });
            }
            catch (Exception ex)
            {
                throw new Exception("封装的异常", ex);
            }
        }

        private async Task ExportShareParaAsync()
        {
            try
            {
                var result = DatagridModels.FirstOrDefault(x => x.IsCheck == true && x.NodeVisibility == Visibility.Visible && x.ParaKind == ParaKindEnum.SharePara);
                if (result == null)
                {
                    MessageBox.Show("请勾选至少一个共享参数", "提示");
                    return;
                }

                int count = 0;
                string filePath;
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.InitialDirectory = @"C:\";
                saveFileDialog.Filter = "文本文件|*.txt";
                if (saveFileDialog.ShowDialog() == true)
                {
                    filePath = saveFileDialog.FileName; //会自动加后缀扩展名
                                                        //检验filePath为已存在内容时重新选择
                }
                else
                {
                    return;
                }

                await RevitTask.RunAsync((uiApp) =>
                {
                    Application serviceApp = uiApp.Application;
                    // 1】创建共享参数文件
                    if (!File.Exists(filePath))
                    {
                        FileStream fileStream = File.Create(filePath);
                        fileStream.Close();
                    }
                    // 2】打开共享族参数定义文件                                
                    serviceApp.SharedParametersFilename = filePath;
                    DefinitionFile shareDefinitionFile = serviceApp.OpenSharedParameterFile();

                    // 3】查找or创建共享族参数分组                                
                    DefinitionGroup shareGroup = shareDefinitionFile.Groups.get_Item("DefaultGroup");
                    if (shareGroup == null)
                    {
                        shareGroup = shareDefinitionFile.Groups.Create("DefaultGroup");
                    }

                    foreach (var curPara in DatagridModels)
                    {
                        if (curPara.IsCheck != true || curPara.NodeVisibility != Visibility.Visible || curPara.ParaKind != ParaKindEnum.SharePara)
                        {
                            continue;
                        }
                        // 4】查找or创建共享族参数的定义               
                        ExternalDefinition shareParameterDef = shareGroup.Definitions.get_Item(curPara.Name) as ExternalDefinition;
                        if (shareParameterDef == null)
                        {
                            ExternalDefinitionCreationOptions externalDefinitionCreationOptions = new ExternalDefinitionCreationOptions(curPara.Name, curPara.ParaType);
                            if (!String.IsNullOrEmpty(curPara.Description))
                            {
                                externalDefinitionCreationOptions.Description = curPara.Description;
                            }
                            if (curPara.HideWhenNoValue != null)
                            {
                                externalDefinitionCreationOptions.HideWhenNoValue = (bool)curPara.HideWhenNoValue;
                            }
                            if (curPara.UserModifiable != null)
                            {
                                externalDefinitionCreationOptions.UserModifiable = (bool)curPara.UserModifiable;
                            }
                            if (curPara.ParaVisibility != null)
                            {
                                externalDefinitionCreationOptions.Visible = (bool)curPara.ParaVisibility;
                            }
                            shareParameterDef = shareGroup.Definitions.Create(externalDefinitionCreationOptions) as ExternalDefinition;
                            count++;
                        }
                    }
                });
                MessageBox.Show($"共导出{count}个共享参数", "提示");               
                Process.Start(Path.GetDirectoryName(filePath));
            }
            catch (Exception ex)
            {
                throw new Exception("封装的异常", ex);
            }
        } 

        #endregion


        public ICommand BatchActionsCommand
        {
            get => new DelegateCommand<ComboBox>((curComboBox) =>
            {
                if (curComboBox.SelectedIndex == 0)
                {
                    return;
                }                
                BatchActionEnum batchAction = HelpEnum.GetEnumByDescription<BatchActionEnum>(((curComboBox.SelectedItem) as ComboModelBase).Name);                
                var result = DatagridModels.FirstOrDefault(x => x.IsCheck == true);
                if (result == null)
                {
                    MessageBox.Show("请至少勾选一个参数", "提示");
                    curComboBox.SelectedIndex = 0;
                    return;
                }
                if (batchAction == BatchActionEnum.CopyProperty)//批量复制属性
                {
                    List<ParaDataGridModel> tempList = new List<ParaDataGridModel>();
                    foreach (ParaDataGridModel item in DatagridModels)
                    {
                        if (item.IsCheck==true)
                        {
                            tempList.Add(item.Clone() as ParaDataGridModel);
                        }
                    }
                    foreach (ParaDataGridModel item in tempList)
                    {
                        DatagridModels.Add(item);
                    }
                }
                else if(batchAction == BatchActionEnum.DeleteProperty)//批量删除属性
                {
                    do
                    {                       
                        DatagridModels.Remove(result);                                           
                        result = DatagridModels.FirstOrDefault(x => x.IsCheck == true);
                    }
                    while (result != null);
                    DataGridAllIsCheck = false;
                }
                curComboBox.SelectedIndex = 0;
            });
        }

        [Dependency]
        public IDialogService dialogService { get; set; }
        public ICommand ModifyCommand
        {
            get => new DelegateCommand(() =>
            {
                var result = DatagridModels.FirstOrDefault(x => x.IsCheck == true);
                if (result == null)
                {
                    MessageBox.Show("请至少勾选一个参数", "提示");                   
                    return;
                }

                DialogParameters dialogParameters = new DialogParameters();
                dialogService.ShowDialog("ModifyParaNameDialogView", dialogParameters, DoDialogResult, "dialogWin");

            });
        }
        private void DoDialogResult(IDialogResult dialogResult)
        {
            try
            {
                if (dialogResult.Result != ButtonResult.OK)
                {
                    return;
                }

                string[] paraModifyTexts = dialogResult.Parameters.GetValue<string[]>("参数编辑信息");
                string prefixText = paraModifyTexts[0];
                string suffixText = paraModifyTexts[1];
                string searchText = paraModifyTexts[2];
                string replaceText = paraModifyTexts[3];

                foreach (var item in DatagridModels)
                {
                    if (item.IsCheck != true)
                    {
                        continue;
                    }
                    string curName = item.Name;
                    if (prefixText != null)
                    {
                        item.Name = string.Concat(prefixText.Trim(), item.Name);
                    }
                    if (suffixText != null)
                    {
                        item.Name = string.Concat(item.Name, suffixText.Trim());
                    }
                    if (searchText == null)
                    {
                        continue;
                    }
                    if (replaceText != null)
                    {
                        replaceText = replaceText.Trim();
                    }
                    if (curName.Contains(searchText.Trim()))
                    {
                        item.Name = item.Name.Replace(searchText.Trim(), replaceText);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Info($"报错信息,{ex}");
                Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
            }
        }

        public ICommand IngoreSelectCommand
        {
            get => new DelegateCommand<SelectionChangedEventArgs>((args) =>
            {               
                ComboBox curComboBox = (args.OriginalSource) as ComboBox;
                curComboBox.SelectedIndex = 0;
            });
        }

        [Dependency]
        public IEventAggregator _eventAggregator { get; set; }

        public ICommand AddtionCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                // 可能原因一：单例模式（是单例模式吗？？？）没有提出对象的销毁。
                //             在提供内存管理的开发语言（比如，基于.NetFramework的语言）中，
                //             只有单例模式对象自己才能将对象实例销毁，因为只有它拥有对实例的引用
                // 可能原因二：析构函数，当 C# 中的所有强引用都被释放并且对象与树断开连接时，它才会被调用——通过控制台调试是否调用了析构函数
                // ✔主要原因：当Revit程序退出时才会依次调用Views、MainWindow的析构函数——且无法通过GC.Collect();GC.SuppressFinalize(this);提前释放——内存泄露问题

                #region 【实现动态列~事件总线~V、VM分层解耦】待日后解决bug：第二次打开插件，Regions中的View会匹配到上一次打开插件生成View的后台代码~关闭窗口时未被销毁

                // 【连锁bug：第二次打开插件时，GetEvent会获取第一次View实例化时被订阅的事件实例】
               _eventAggregator.GetEvent<DynamicColumnEvent>().Publish(obj);
               
                #endregion

            });
        }

        public ICommand DocConfirmCommand
        {
            get => new DelegateCommand<RoutedEventArgs>(async (args) =>
            {
                try
                {
                    if (!(args.OriginalSource is Border))
                    {
                        return;
                    }
                    Border curBorder = args.OriginalSource as Border;
                    if (curBorder.Name != "templateRoot")
                    {
                        return;
                    }                   
                    ComboBox curComboBox = args.Source as ComboBox;
                    if (curComboBox.SelectedIndex == 1)
                    {
                        await LoadNestedFamily();                       
                    }
                    args.Handled = true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }               
            });
        }
        public ICommand DocSelectedCommand
        {
            get => new DelegateCommand<SelectionChangedEventArgs>(async (args) =>
            {
                try
                {                   
                    ComboBox curComboBox = (args.OriginalSource) as ComboBox;

                    if (curComboBox.SelectedIndex == 0)
                    {
                        SysCache.Instance.CurFamily = null;
                        SysCache.Instance.LoadFamilyTreeSourceEventHandler.Execute(SysCache.Instance.ExternEventExecuteApp);
                        InitTree();
                    }
                    else if (curComboBox.SelectedIndex == 1)
                    {
                        await LoadNestedFamily();                        
                    }                   
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }
            });
        }

        #region 辅助方法——载入嵌套族初始化TreeView
        private async Task LoadNestedFamily()
        {
            try
            {
                Element ele = null;
                await RevitTask.RunAsync((uiApp) =>
                {
                    //选择族—— Reference类：对该构件(element)或者基本几何（line,face）的指代，一个载体
                    UIDocument uidoc = uiApp.ActiveUIDocument;
                    Document doc = uidoc.Document;
                    Reference reference = null;
                    try
                    {
                        reference = uidoc.Selection.PickObject(ObjectType.Element, new LoadFamilySelectionFilter(), "请选择一个载入族"); //参数：指定类型，过滤器,提示信息
                    }
                    catch
                    {
                        return;
                    }
                    ele = doc.GetElement(reference);
                });
                if (ele == null)
                {
                    return;
                }
                SysCache.Instance.CurFamily = (ele as FamilyInstance).Symbol.Family;
                SysCache.Instance.LoadFamilyTreeSourceEventHandler.Execute(SysCache.Instance.ExternEventExecuteApp);
                InitTree();
            }
            catch (Exception ex)
            {
                throw new Exception("封装的异常", ex);
            }
        }
        #endregion



        private string _SearchParaText = "";//避免String.Contains(null)报错
        public string SearchParaText
        {
            get { return _SearchParaText; }
            set { SetProperty(ref _SearchParaText, value, SrerchParaTextChanged); }
        }
        private void SrerchParaTextChanged()
        {
            int index = 0;//默认全选
            ParaKindEnum curParaKind=default(ParaKindEnum);
            if (ShareOrFamilyModels[0].IsCheck == null)//共享or族参数
            {
                CheckAndComboModel result = ShareOrFamilyModels.FirstOrDefault(x => x.IsCheck == true);
                index = ShareOrFamilyModels.IndexOf(result);
                curParaKind = HelpEnum.GetEnumByDescription<ParaKindEnum>(result.Name);
            }
            else if (ShareOrFamilyModels[0].IsCheck == false)//无
            {
                return;
            }

            foreach (var item in DatagridModels)
            {
                if (!item.Name.Contains(SearchParaText))
                {
                    item.NodeVisibility = Visibility.Collapsed;
                    continue;
                }
                else
                {
                    item.NodeVisibility = Visibility.Visible;
                }
                if (index == 0)
                {
                    continue;
                }
                if (item.ParaKind != curParaKind)
                {
                    item.NodeVisibility = Visibility.Collapsed;
                }
            }
        }

        #endregion


        #region DataGrid交互
        public ObservableCollection<ParaDataGridModel> DatagridModels { get; set; } = new ObservableCollection<ParaDataGridModel>();

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
                foreach (var item in DatagridModels)
                {
                    item.IsCheck = DataGridAllIsCheck;
                }
            });
        }

        public ICommand ItemCheckChangedCommand
        {
            get => new DelegateCommand<Object>((obj) =>
            {
                ParaDataGridModel curItem = obj as ParaDataGridModel;
                DataGridAllIsCheck = curItem.IsCheck;
                foreach (var item in DatagridModels)
                {
                    if (item.IsSelect == true)
                    {
                        item.IsCheck = curItem.IsCheck;
                    }
                    if (item.IsCheck != curItem.IsCheck && DataGridAllIsCheck != null)
                    {
                        DataGridAllIsCheck = null;
                    }
                }
            });
        }

        public ObservableCollection<CheckAndComboModel> ShareOrFamilyModels { get; set; } = new ObservableCollection<CheckAndComboModel>();

        public ICommand ShareOrFamCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                try
                {
                    CheckAndComboModel curAttribute = obj as CheckAndComboModel;
                    bool curValue;
                    if (ShareOrFamilyModels[0].IsCheck == null)//此时为全选或全不选
                    {
                        curValue = (bool)curAttribute.IsCheck;
                        ShareOrFamilyModels[0].IsCheck = curValue;
                        ShareOrFamilyModels[0].IsSelect = curValue;
                        ShareOrFamilyModels[1].IsSelect = false;
                        ShareOrFamilyModels[2].IsSelect = false;
                        for (int i = 1; i < ParaGroupModels.Count; i++)
                        {
                            ParaGroupModels[i].IsSelect = false;
                        }
                        if (curValue)//全选
                        {
                            foreach (var item in DatagridModels)
                            {                               
                                if (item.NodeVisibility != Visibility.Visible && item.Name.Contains(SearchParaText))
                                {
                                    item.NodeVisibility = Visibility.Visible;
                                }
                            }
                        }
                        else//全不选
                        {
                            foreach (var item in DatagridModels)
                            {
                                if (item.NodeVisibility == Visibility.Visible)
                                {
                                    item.NodeVisibility = Visibility.Collapsed;
                                }
                            }
                        }
                    }
                    else//只选了一项
                    {
                        ShareOrFamilyModels[0].IsCheck = null;                        
                        ParaKindEnum curParaKind = default(ParaKindEnum);
                        foreach (var item in ShareOrFamilyModels)
                        {                            
                            if (item.IsCheck == true)
                            {
                                item.IsSelect = true;
                                curParaKind = HelpEnum.GetEnumByDescription<ParaKindEnum>(item.Name);
                                break;
                            }
                            item.IsSelect = false;
                        }
                        foreach (var item in DatagridModels)
                        {
                            if (item.ParaKind == curParaKind && item.Name.Contains(SearchParaText))
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

        public ICommand SelectionChangedCommand
        {
            get => new DelegateCommand<SelectionChangedEventArgs>((args) =>
            {
                try
                {
                    ComboBox curComboBox = (args.OriginalSource) as ComboBox;                    
                    //获取当前DataGrid绑定项
                    ParaDataGridModel curDataGridModel = (curComboBox.DataContext) as ParaDataGridModel;

                    object curItem = curComboBox.SelectedItem;                    
                    if (curItem == null)//IsShareParameter为false时，部分项为null情况
                    {
                        return;
                    }

                    //var curValue = curComboBox.SelectedValue;//【bug】获取的不是实时更新的值：可能为null(ParameterType为valid)——解决：通过反射获取curItem对应属性值
                    PropertyInfo curPropertyInfo = curItem.GetType().GetProperty(curComboBox.SelectedValuePath);
                    var curValue = curPropertyInfo.GetValue(curItem);

                    string curPropertyName;
                    PropertyInfo aimPropertyInfo;
                    if (curItem is BoolAndComboModel)
                    {
                        curPropertyName = (curItem as BoolAndComboModel).BindingName;
                        aimPropertyInfo = typeof(ParaDataGridModel).GetProperty(curPropertyName);
                        foreach (var item in DatagridModels)
                        {
                            if (item.IsSelect != true)
                            {
                                continue;
                            }
                            if (curPropertyName == "IsInstancePara")
                            {
                                aimPropertyInfo.SetValue(item, curValue);
                            }
                            else
                            {
                                if (item.IsShareParameter)
                                {
                                    aimPropertyInfo.SetValue(item, curValue);
                                }
                            }
                        }
                        DatagridModels.ToList().ForEach(item =>
                        {           
                                                    
                        });
                    }
                    else if (curItem is ParaTypeAndComboModel)
                    {
                        curPropertyName = curComboBox.SelectedValuePath;
                        aimPropertyInfo = typeof(ParaDataGridModel).GetProperty(curPropertyName);
                        foreach (var item in DatagridModels)
                        {
                            if (item == curDataGridModel)//排除自身项
                            {
                                continue;
                            }
                            if (item.Discipline != curDataGridModel.Discipline)//注意：需排除不同规程的参数
                            {
                                continue;
                            }
                            if (item.IsSelect == true)
                            {
                                aimPropertyInfo.SetValue(item, curValue);

                            }
                        }
                    }
                    else
                    {
                        curPropertyName = curComboBox.SelectedValuePath;
                        aimPropertyInfo = typeof(ParaDataGridModel).GetProperty(curPropertyName);   
                        foreach(var item in DatagridModels)
                        {
                            if (item== curDataGridModel)//排除自身项
                            {
                                continue;
                            }
                            if (item.IsSelect == true)
                            {
                                aimPropertyInfo.SetValue(item, curValue);

                            }
                        }                      
                    }                                       
                    //【扩展：可通过aimPropertyInfo.PropertyType获得属性的Type】                                                                                             
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }                
            });
        }

        public ObservableCollection<ParaKindAndComboModel> ShareOrFamilyOfKindModels { get; set; } = new ObservableCollection<ParaKindAndComboModel>();
        public ObservableCollection<DisciplineAndComboModel> DisciplineModels { get; set; } = new ObservableCollection<DisciplineAndComboModel>();

        public ObservableCollection<ParaGroupAndComboModel> ParaGroupModels { get; set; } = new ObservableCollection<ParaGroupAndComboModel>();

        public ObservableCollection<BoolAndComboModel> InstanceOrTypeModels { get; set; } = new ObservableCollection<BoolAndComboModel>();

        public ObservableCollection<BoolAndComboModel> ParaVisibleModels { get; set; } = new ObservableCollection<BoolAndComboModel>();

        public ObservableCollection<BoolAndComboModel> IsModifiableModels { get; set; } = new ObservableCollection<BoolAndComboModel>();

        public ObservableCollection<BoolAndComboModel> IsHideModels { get; set; } = new ObservableCollection<BoolAndComboModel>();
        #endregion


        #region TreeView交互

        // TreeView的根节点        
        public ObservableCollection<CatagoryTreeNode> RootTreeModels { get; set; } = new ObservableCollection<CatagoryTreeNode>();

        private bool? _TreeAllIsCheck = false;
        public bool? TreeAllIsCheck //null表示未全选该节点的所有子节点
        {
            get { return _TreeAllIsCheck; }
            set { SetProperty(ref _TreeAllIsCheck, value); }
        }


        public ICommand AllNodeCheckChangedCommand
        {
            get => new DelegateCommand(() =>
            {
                DatagridModels.ToList().ForEach(x=>
                {
                    x.IsApplyError = null;
                });
                foreach (var peerNode in RootTreeModels)
                {
                    if (peerNode.IsCheck != TreeAllIsCheck && peerNode.NodeVisibility == Visibility.Visible)
                    {
                        TreeViewHelper.SelectDown(peerNode, (bool)TreeAllIsCheck);
                    }
                }
            });
        }

        private string _SrerchFamilyText;
        public string SrerchFamilyText
        {
            get { return _SrerchFamilyText; }
            set { SetProperty(ref _SrerchFamilyText, value, SrerchFamilyTextChanged); }
        }
        private void SrerchFamilyTextChanged()
        {
            DatagridModels.ToList().ForEach(x =>
            {
                x.IsApplyError = null;
            });

            if (SrerchFamilyText == "" || SrerchFamilyText == null)
            {
                RootTreeModels.ToList().ForEach(x =>
                {
                    TreeViewHelper.RecursiveTraverseTree(x, (item) =>
                    {
                        item.NodeVisibility = Visibility.Visible;
                        if (item.IsCheck != false)
                        {
                            item.IsExpand = true;
                        }
                        else
                        {
                            item.IsExpand = false;
                        }
                    });
                });
                return;
            }
            RootTreeModels.ToList().ForEach(x =>
            {
                TreeViewHelper.RecursiveTraverseTree(x, (item) =>
                {
                    if (item.Name.Contains(SrerchFamilyText.Trim()))
                    {
                        item.NodeVisibility = Visibility.Visible;
                        item.IsExpand = true;
                        TreeViewHelper.ShowSearchUp(item);
                        TreeViewHelper.ShowSearchDown(item);
                    }
                    else
                    {
                        if (item.NodeType == TreeNodeType.Catagory)
                        {
                            item.NodeVisibility = Visibility.Collapsed;
                        }
                        item.IsExpand = false;
                    }
                });
            });
        }

        public ICommand NodeCheckChangedCommand
        {
            get => new DelegateCommand<IBaseModel>((treeNode) =>
            {
                try
                {
                    DatagridModels.ToList().ForEach(x =>
                    {
                        x.IsApplyError = null;
                    });

                    bool newIsCheckValue;
                    if (treeNode.IsCheck == true)
                    {
                        newIsCheckValue = false;
                    }
                    else
                    {
                        newIsCheckValue = true;
                    }
                    //递归检查
                    TreeViewHelper.SelectDown(treeNode, newIsCheckValue);
                    TreeViewHelper.SelectUp(treeNode, (bool)TreeAllIsCheck, RootTreeModels);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }
            });
        }





        #endregion


        #region 进度条、重置、应用

        /// 进度条属性       
        private double _CurProgressBarValue = 0;
        public double CurProgressBarValue
        {
            get { return _CurProgressBarValue; }
            set { SetProperty(ref _CurProgressBarValue, value); }
        }

        private Visibility _ProgressBarVisible = Visibility.Collapsed;
        public Visibility ProgressBarVisible
        {
            get { return _ProgressBarVisible; }
            set { SetProperty(ref _ProgressBarVisible, value); }
        }

        private string _CurProgressBarDisplay;
        public string CurProgressBarDisplay
        {
            get { return _CurProgressBarDisplay; }
            set { SetProperty(ref _CurProgressBarDisplay, value); }
        }

        public ICommand UnioCommand
        {
            get => new DelegateCommand(() =>
            {
                MessageBoxResult result =MessageBox.Show("是否要清除所有参数？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result== MessageBoxResult.Yes)
                {
                    DatagridModels.Clear();
                    dictFamilyToParaCreated.Clear();
                    DataGridAllIsCheck = false;
                }
            });
        }

        private Dictionary<string, List<string>> dictFamilyToParaCreated = new Dictionary<string, List<string>>();
        private IList<ParaCreateDataGridModel> paraCreateds = new List<ParaCreateDataGridModel>();
        public ICommand ApplyCommand
        {
            get => new DelegateCommand(async () =>
            {
                try
                {
                    /// 异常情况：排除未选择族或参数的情况
                    var resultFamily = RootTreeModels.FirstOrDefault(x => x.IsCheck != false && x.NodeVisibility == Visibility.Visible);
                    if (resultFamily == null)
                    {
                        MessageBox.Show("请至少勾选一个族", "提示");
                        return;
                    }
                    var resultPara = DatagridModels.FirstOrDefault(x => x.IsCheck == true && x.IsApplyError != false);
                    if (resultPara == null)
                    {
                        MessageBox.Show("请至少勾选一个未被成功创建的参数", "提示");
                        return;
                    }


                    ///前期准备：归零并显示进度条               
                    IList<string> tempFamilyList = new List<string>();
                    IList<ParaDataGridModel> tempParaList = new List<ParaDataGridModel>();
                    foreach (var item in RootTreeModels)
                    {
                        if (item.IsCheck == false || item.NodeVisibility != Visibility.Visible)
                        {
                            continue;
                        }
                        item.TreeNodes.ToList().ForEach(x =>
                        {
                            if (x.IsCheck == true)
                            {
                                tempFamilyList.Add(x.Name);
                            }
                        });
                    }
                    foreach (var item in DatagridModels)
                    {
                        if (item.IsCheck != true || item.IsApplyError == false)//排除已经成功创建的参数
                        {
                            continue;
                        }
                        item.IsApplyError = null;// 复位DataGrid子项参数创建状态
                        tempParaList.Add(item);
                    }

                    double curExportCount = 0;
                    double maxCount = tempFamilyList.Count * tempParaList.Count;
                    ProgressBarVisible = Visibility.Visible;


                    /// 参数创建
                    string shareParameterGroupName = "Family-ShareParameterGroup";
                    foreach (string curFamilyName in tempFamilyList)
                    {
                        bool isOrderExecute = true;//标志位
                        Document curDoc = null;
                        Document familyDoc = null;
                        //获取相关文档
                        await RevitTask.RunAsync((uiApp) =>
                        {
                            //【在这里执行同步代码，不返回任何结果，通过uiApp参数访问Revit模型数据库】

                            if (SysCache.Instance.CurFamily == null)
                            {
                                curDoc = uiApp.ActiveUIDocument.Document;
                            }
                            else
                            {
                                curDoc = uiApp.ActiveUIDocument.Document.EditFamily(SysCache.Instance.CurFamily);
                            }
                            Application serviceApp = uiApp.Application;
                            Family family = new FilteredElementCollector(curDoc).OfClass(typeof(Family)).FirstOrDefault(x => x.Name == curFamilyName) as Family;//根据族名字过滤获取项目中的族
                            if (family == null)
                            {
                                isOrderExecute = false;
                                curExportCount += tempParaList.Count;
                                return;
                            }
                            // 注意：
                            // doc.IsModifiable为true时（文档的事务未关闭），将不会调用EditFamily方法【IsModifiable不等于IsModified】
                            // doc.IsReadOnly为true时（只读状态），也不会调用EditFamily方法
                            // family为内建族(family.IsInPlace==true)或不可编辑族(family.IsEditable==false)时会抛出该族，无法获取族文档
                            familyDoc = curDoc.EditFamily(family);
                            if (familyDoc == null)
                            {
                                isOrderExecute = false;
                                curExportCount += tempParaList.Count;
                                return;
                            }
                        });
                        if (!isOrderExecute)
                        {
                            continue;
                        }

                        //依次创建族的系列参数
                        foreach (ParaDataGridModel curPara in tempParaList)
                        {
                            CurProgressBarValue = (curExportCount / maxCount) * 100;
                            CurProgressBarDisplay = $"进度：{CurProgressBarValue:f1}%-{curFamilyName}-{curPara.Name}";
                            ParaCreateDataGridModel curParaCreated = new ParaCreateDataGridModel()
                            {
                                FamilyName = curFamilyName,
                                ParaName = curPara.Name
                            };

                            if (curPara.IsApplyError == null)
                            {
                                curPara.IsApplyError = false;
                            }
                            if (dictFamilyToParaCreated.ContainsKey(curFamilyName))// 过滤已经创建成功的参数
                            {
                                if (dictFamilyToParaCreated[curFamilyName].Contains(curPara.Name))
                                {
                                    curParaCreated.Description = "该参数已创建成功，无需重复创建";
                                    paraCreateds.Add(curParaCreated);
                                    curExportCount++;
                                    continue;
                                }
                            }

                            await RevitTask.RunAsync((uiApp) =>
                            {
                                Application serviceApp = uiApp.Application;
                                FamilyManager familyManager = familyDoc.FamilyManager;
                                Transaction trans = new Transaction(familyDoc, "添加参数");

                                FamilyParameter familyParameter = null;
                                try
                                {
                                    if (curPara.ParaKind == ParaKindEnum.SharePara)//共享族参数
                                    {
                                        // 1】创建共享参数文件
                                        string curPath = Assembly.GetExecutingAssembly().Location;
                                        string curFolder = Path.GetDirectoryName(curPath);
                                        string shareParameterFilePath = curFolder + "\\ShareParameter.txt";
                                        if (!File.Exists(shareParameterFilePath))
                                        {
                                            FileStream fileStream = File.Create(shareParameterFilePath);
                                            fileStream.Close();
                                        }

                                        // 2】打开共享族参数定义文件                                
                                        serviceApp.SharedParametersFilename = shareParameterFilePath;
                                        DefinitionFile shareDefinitionFile = serviceApp.OpenSharedParameterFile();

                                        // 3】查找or创建共享族参数分组                                
                                        DefinitionGroup shareGroup = shareDefinitionFile.Groups.get_Item(shareParameterGroupName);
                                        if (shareGroup == null)
                                        {
                                            shareGroup = shareDefinitionFile.Groups.Create(shareParameterGroupName);
                                        }

                                        // 4】查找or创建共享族参数的定义
                                        bool isReCreateExternalDef = false;//标志位
                                        ExternalDefinition shareParameterDef = shareGroup.Definitions.get_Item(curPara.Name) as ExternalDefinition;
                                        if (shareParameterDef == null)
                                        {
                                            isReCreateExternalDef = true;
                                        }
                                        else
                                        {                                                                                     
                                            if (shareParameterDef.ParameterType != curPara.ParaType || shareParameterDef.Description != curPara.Description || shareParameterDef.HideWhenNoValue != curPara.HideWhenNoValue || shareParameterDef.UserModifiable != curPara.UserModifiable || shareParameterDef.Visible != curPara.ParaVisibility)
                                            {
                                                isReCreateExternalDef = true;
                                                int tempCount = 0;
                                                string tempShareGroupName;
                                                do
                                                {
                                                    tempCount++;
                                                    tempShareGroupName = shareParameterGroupName + "-temp" + tempCount.ToString();
                                                    shareGroup = shareDefinitionFile.Groups.get_Item(tempShareGroupName);
                                                    if (shareGroup == null)
                                                    {
                                                        shareGroup = shareDefinitionFile.Groups.Create(tempShareGroupName);
                                                        break;
                                                    }
                                                    shareParameterDef = shareGroup.Definitions.get_Item(curPara.Name) as ExternalDefinition;
                                                    if (shareParameterDef == null)
                                                    {
                                                        break;
                                                    }
                                                }
                                                while (true);                                               
                                            }
                                            // 因为无法删除以及修改已创建的ExternalDefinition——临时解决方法：在新建定义组中创建同名的ExternalDefinition
                                           
                                        }
                                        if (isReCreateExternalDef)
                                        {
                                            ExternalDefinitionCreationOptions externalDefinitionCreationOptions = new ExternalDefinitionCreationOptions(curPara.Name, curPara.ParaType);
                                            //【附加——共享族参数的附加属性：HideWhenNoValue、UserModifiableGUID、Visible、Description~通过ExternalDefinitionCreationOptions设置】
                                            // 需判断相关属性是否一致——不一致需重新创建
                                            if (!String.IsNullOrEmpty(curPara.Description))
                                            {
                                                externalDefinitionCreationOptions.Description = curPara.Description;
                                            }
                                            if (curPara.HideWhenNoValue != null)
                                            {
                                                externalDefinitionCreationOptions.HideWhenNoValue = (bool)curPara.HideWhenNoValue;
                                            }
                                            if (curPara.UserModifiable != null)
                                            {
                                                externalDefinitionCreationOptions.UserModifiable = (bool)curPara.UserModifiable;
                                            }
                                            if (curPara.ParaVisibility != null)
                                            {
                                                externalDefinitionCreationOptions.Visible = (bool)curPara.ParaVisibility;
                                            }
                                            shareParameterDef = shareGroup.Definitions.Create(externalDefinitionCreationOptions) as ExternalDefinition;
                                        }
                                        trans.Start();
                                        familyParameter = familyManager.AddParameter(shareParameterDef, curPara.ParaGroup, curPara.IsInstancePara);//族类型必须是嵌套子族的类别
                                        trans.Commit();
                                    }
                                    else//一般族参数
                                    {
                                        trans.Start();
                                        familyParameter = familyManager.AddParameter(curPara.Name, curPara.ParaGroup, curPara.ParaType, curPara.IsInstancePara);//族类型必须是嵌套子族的类别                                   
                                        if (!String.IsNullOrEmpty(curPara.Description))
                                        {
                                            familyManager.SetDescription(familyParameter, "xxxdescription");
                                        }
                                        trans.Commit();
                                    }

                                    // 参数公式设置
                                    try
                                    {
                                        if (!String.IsNullOrEmpty(curPara.ParaFormula))
                                        {
                                            string curFormula = curPara.ParaFormula.Trim();
                                            trans.Start();
                                            familyManager.SetFormula(familyParameter, curFormula);
                                            trans.Commit();
                                        }
                                        // 参数值设置
                                        try
                                        {
                                            if (!String.IsNullOrEmpty(curPara.ParaValue))
                                            {
                                                string curValue = curPara.ParaValue.Trim();
                                                trans.Start();
                                                if (familyManager.CurrentType != null)//待解决：排除无CurrentType情况
                                                {
                                                    //【注意】值仅仅会赋值给CurrentType，需遍历每一个类型
                                                    FamilyTypeSet familyTypeSet = familyManager.Types;
                                                    foreach (FamilyType familyType in familyTypeSet)
                                                    {
                                                        familyManager.CurrentType = familyType;
                                                        if (familyParameter.StorageType == StorageType.String)
                                                        {
                                                            familyManager.Set(familyParameter, curValue);
                                                        }
                                                        else
                                                        {
                                                            familyManager.SetValueString(familyParameter, curValue);
                                                        }
                                                    }                                                   
                                                }
                                                trans.Commit();
                                            }
                                        }
                                        catch
                                        {
                                            familyManager.RemoveParameter(familyParameter);
                                            trans.Commit();
                                            if (curPara.IsApplyError != true)
                                            {
                                                curPara.IsApplyError = true;
                                            }
                                            curParaCreated.IsCreatedSuccess = false;
                                            curParaCreated.Description = "该参数未创建成功，参数值设置出错";
                                        }
                                    }
                                    catch
                                    {
                                        familyManager.RemoveParameter(familyParameter);
                                        trans.Commit();
                                        if (curPara.IsApplyError != true)
                                        {
                                            curPara.IsApplyError = true;
                                        }
                                        curParaCreated.IsCreatedSuccess = false;
                                        curParaCreated.Description = "该参数未创建成功，参数公式设置出错";
                                    }
                                }
                                catch
                                {
                                    trans.Commit();
                                    if (curPara.IsApplyError != true)
                                    {
                                        curPara.IsApplyError = true;
                                    }
                                    curParaCreated.IsCreatedSuccess = false;
                                    curParaCreated.Description = "该参数未创建成功，族中已存在同名的参数";
                                }

                                curExportCount++;
                                paraCreateds.Add(curParaCreated);
                                if (!familyManager.Parameters.Contains(familyParameter))//未成功创建
                                {
                                    return;
                                }
                                if (!dictFamilyToParaCreated.ContainsKey(curFamilyName))//记录到已成功创建的族_参数映射中
                                {
                                    dictFamilyToParaCreated.Add(curFamilyName, new List<string>());
                                }
                                dictFamilyToParaCreated[curFamilyName].Add(curPara.Name);
                            });
                        }

                        //保存至相关文档
                        await RevitTask.RunAsync((uiApp) =>
                        {                                                                           
                            if (SysCache.Instance.CurFamily != null)
                            {
                                string famName = SysCache.Instance.CurFamily.Name;
                                familyDoc.LoadFamily(curDoc, new FamilyLoadOptions()); //若项目中已存在相同的族文档，则需实现接口IFamilyLoadOptions中的方法    
                                curDoc.LoadFamily(uiApp.ActiveUIDocument.Document, new FamilyLoadOptions());
                                //【注意】 还需更新相应的族文档对应的族，否则在下次参数创建，找不到相应的族文档（1492行将报错）                               
                                Document docProject = uiApp.ActiveUIDocument.Document;
                                SysCache.Instance.CurFamily = new FilteredElementCollector(docProject).OfClass(typeof(Family)).FirstOrDefault(x => x.Name == famName) as Family;//根据族名字过滤获取项目中的族
                            }
                            else
                            {
                                familyDoc.LoadFamily(curDoc, new FamilyLoadOptions());
                            }
                        });
                    }

                    CurProgressBarValue = (curExportCount / maxCount) * 100;
                    CurProgressBarDisplay = $"进度：{CurProgressBarValue:f1}%";

                    DialogParameters dialogParameters = new DialogParameters();
                    dialogParameters.Add("参数创建情况", paraCreateds);//待优化：字符串引入常量
                    dialogService.ShowDialog("ParaCreateDialogView", dialogParameters, DoDialogForParaCreateResult, "dialogWinForParaCreate");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }
            });
        }
        private void DoDialogForParaCreateResult(IDialogResult dialogResult)
        {
            ProgressBarVisible = Visibility.Collapsed;//隐藏进度条
            CurProgressBarValue = 0;
            CurProgressBarDisplay = "进度：0%";
            paraCreateds.Clear();
            DatagridModels.ToList().ForEach(x =>
            {
                if (x.IsSelect)
                {
                    x.IsSelect = false;
                }
            });
        }
        #endregion


        #region 辅助函数——初始化TreeView、ComboBox       

        private void InitComboBox()
        {
            //添加参数_ComboBox
            AddParameterModels.Add(new ComboModelBase() { Name = "添加参数" });
            AddParameterModels.Add(new ComboModelBase() { Name = "参数迁移" });

            //参数导入/导出_ComboBox
            foreach (ImportOrExportEnum value in Enum.GetValues(typeof(ImportOrExportEnum)))
            {
                ComboModelBase curModel = HelpEnum.GetModel<ImportOrExportEnum, ComboModelBase>(value) as ComboModelBase;
                if (curModel !=null)
                {
                    ImportOrExportModels.Add(curModel);
                }
            }

            //参数批量操作_ComboBox
            foreach (BatchActionEnum value in Enum.GetValues(typeof(BatchActionEnum)))
            {
                ComboModelBase curModel = HelpEnum.GetModel<BatchActionEnum, ComboModelBase>(value) as ComboModelBase;
                if (curModel != null)
                {
                    BatchActionModels.Add(curModel);
                }
            }

            //参数附加属性_ComboBox
            foreach (AdditionPropertyEnum value in Enum.GetValues(typeof(AdditionPropertyEnum)))
            {
                CheckAndComboModel curModel = HelpEnum.GetModel<AdditionPropertyEnum, CheckAndComboModel>(value) as CheckAndComboModel;
                if (curModel != null)
                {
                    AdditionPropertyModels.Add(curModel);
                }
            }

            //源文档_ComboBox
            DocSourceModels.Add(new ComboModelBase() { Name = "当前文档" });
            DocSourceModels.Add(new ComboModelBase() { Name = "嵌套族" });

            //DataGrid_表头_参数类别_ComboBox
            CheckAndComboModel modelOne = HelpEnum.GetModel<ParaKindEnum, CheckAndComboModel>(ParaKindEnum.AllKind) as CheckAndComboModel;
            if (modelOne != null)
            {
                modelOne.IsCheck = true;
                modelOne.IsSelect = true;
                ShareOrFamilyModels.Add(modelOne);
            }
            CheckAndComboModel modelTwo = HelpEnum.GetModel<ParaKindEnum, CheckAndComboModel>(ParaKindEnum.FamilyPara) as CheckAndComboModel;
            if (modelTwo != null)
            {
                modelTwo.IsCheck = true;
                ShareOrFamilyModels.Add(modelTwo);
            }
            CheckAndComboModel modelThree = HelpEnum.GetModel<ParaKindEnum, CheckAndComboModel>(ParaKindEnum.SharePara) as CheckAndComboModel;
            if (modelThree != null)
            {
                modelThree.IsCheck = true;
                ShareOrFamilyModels.Add(modelThree);
            }

            //DataGrid_参数类别_ComboBox
            ParaKindAndComboModel kindOne = HelpEnum.GetModel<ParaKindEnum, ParaKindAndComboModel>(ParaKindEnum.FamilyPara) as ParaKindAndComboModel;
            if (kindOne != null)
            {
                ShareOrFamilyOfKindModels.Add(kindOne);
            }
            ParaKindAndComboModel kindTwo = HelpEnum.GetModel<ParaKindEnum, ParaKindAndComboModel>(ParaKindEnum.SharePara) as ParaKindAndComboModel;
            if (kindTwo != null)
            {
                ShareOrFamilyOfKindModels.Add(kindTwo);
            }

            //DataGrid_参数规程_ComboBox
            foreach (KeyValuePair<string, Dictionary<string, string>> ug in SysCache.Instance.DiscipToParaTypeToBuiltInGruop)
            {
                DisciplineEnum disciplineEnum;
                if (!Enum.TryParse(ug.Key, out disciplineEnum))
                {
                    continue;
                }
                DisciplineAndComboModel curModel = HelpEnum.GetModel<DisciplineEnum, DisciplineAndComboModel>(disciplineEnum) as DisciplineAndComboModel;
                if (curModel != null)
                {
                    DisciplineModels.Add(curModel);
                }
            }

            //DataGrid_参数内置分组_ComboBox
            foreach (BuiltInParameterGroup item in SysCache.Instance.EditableBuiltInParaGroup)
            {
                ParaGroupModels.Add(new ParaGroupAndComboModel()
                {
                    Name = LabelUtils.GetLabelFor(item),
                    ParaGroup = item
                });
            }

            foreach (bool b in new[] { true, false })
            {
                string instanceOrTypeMame;
                string boolMame;
                if (b)
                {
                    instanceOrTypeMame = "实例";
                    boolMame = "是";
                }
                else
                {
                    instanceOrTypeMame = "类型";
                    boolMame = "否";
                }

                InstanceOrTypeModels.Add(new BoolAndComboModel()
                {
                    Name = instanceOrTypeMame,
                    BoolValue = b,
                    BindingName = "IsInstancePara"
                });

                ParaVisibleModels.Add(new BoolAndComboModel()
                {
                    Name = boolMame,
                    BoolValue = b,
                    BindingName= "ParaVisibility"
                });

                IsModifiableModels.Add(new BoolAndComboModel()
                {
                    Name = boolMame,
                    BoolValue = b,
                    BindingName = "UserModifiable"

                });

                IsHideModels.Add(new BoolAndComboModel()
                {
                    Name = boolMame,
                    BoolValue = b,
                    BindingName = "HideWhenNoValue"
                });
            }
        }

        public void InitTree()
        {
            RootTreeModels.Clear();
            if (SysCache.Instance.FamilyTreeSourceList.Count == 0)
            {
                MessageBox.Show("该文档中无载入族","提示");
                return;
            }
            foreach (var curfamilyType in SysCache.Instance.FamilyTreeSourceList)
            {
                var result = RootTreeModels.FirstOrDefault(x => x.Name == curfamilyType.Category.Name);
                if (result == null)
                {
                    CatagoryTreeNode catagoryTreeModel = new CatagoryTreeNode()//类别节点
                    {
                        Name = curfamilyType.Category.Name
                    };
                    var familyElements = from element in SysCache.Instance.FamilyTreeSourceList
                                         where element.Category.Name == curfamilyType.Category.Name
                                         select element;

                    IList<string> FamilyNameList = new List<string>();//临时存储该类别所有族名
                    foreach (var CurFamilyElement in familyElements)
                    {
                        string curFamilyName = (CurFamilyElement as ElementType).FamilyName;
                        if (curFamilyName != "" && !FamilyNameList.Contains(curFamilyName))//过滤掉无族名的情况
                        {
                            FamilyNameList.Add(curFamilyName);
                        }
                    }

                    foreach (var familyName in FamilyNameList)
                    {
                        FamilyTreeNode familyTreeModel = new FamilyTreeNode()//族节点
                        {
                            Name = familyName,
                            Parent = catagoryTreeModel
                        };
                        catagoryTreeModel.TreeNodes.Add(familyTreeModel);
                    }
                    RootTreeModels.Add(catagoryTreeModel);
                }
            }
        }

        #endregion


    }
}
