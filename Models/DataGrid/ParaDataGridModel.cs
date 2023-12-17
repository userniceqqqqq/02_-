using Autodesk.Revit.DB;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Visibility = System.Windows.Visibility;

namespace ParameterManager
{
    public class ParaDataGridModel : BindableBase, ICloneable
    {
        private int count = 0;
        public object Clone()
        {
            ParaDataGridModel paraDataGridModel = new ParaDataGridModel();

            paraDataGridModel.IsCheck = this.IsCheck;
            paraDataGridModel.IsShareParameter = this.IsShareParameter;
            this.count++;
            paraDataGridModel.Name = this.Name + $"副本{this.count}";
            paraDataGridModel.ParaKind = this.ParaKind;
            paraDataGridModel.Discipline = this.Discipline;
            paraDataGridModel.ParaType = this.ParaType;
            paraDataGridModel.ParaGroup = this.ParaGroup;
            paraDataGridModel.IsInstancePara = this.IsInstancePara;
            paraDataGridModel.ParaValue = this.ParaValue;
            paraDataGridModel.ParaFormula = this.ParaFormula;

            paraDataGridModel.Description = this.Description;
            paraDataGridModel.HideWhenNoValue = this.HideWhenNoValue;
            paraDataGridModel.UserModifiable = this.UserModifiable;
            paraDataGridModel.ParaVisibility = this.ParaVisibility;

            return paraDataGridModel;
        }

        public ParaDataGridModel()
        {
            Init(Discipline);
            ParaType = (ParaTypeModels.FirstOrDefault() as ParaTypeAndComboModel).ParaType;
        }

        private void Init(UnitGroup unitGroup)
        {
            foreach (ParameterType curParameterType in SysCache.Instance.UnitGroupToParameterType[unitGroup])
            {
                // 注意：ParameterType.FamilyType无通过LabelUtils.GetLabelFor获取中文名
                if (curParameterType == ParameterType.FamilyType)
                {
                    //需选定其嵌套族的族类别作为其参数类型——在批量处理时可考虑注释掉该参数类型
                    //ParaTypeModels.Add(new ParaTypeAndComboModel()
                    //{
                    //    Name = "族类别",
                    //    ParaType = curParameterType
                    //});
                    continue;
                }
                ParaTypeModels.Add(new ParaTypeAndComboModel()
                {
                    Name = LabelUtils.GetLabelFor(curParameterType),
                    ParaType = curParameterType
                });
            }
        }

        #region DataGrid交互

        private bool? _IsCheck = false;
        public bool? IsCheck //null表示未全选该节点的所有子节点
        {
            get { return _IsCheck; }
            set { SetProperty(ref _IsCheck, value); }
        }

        private bool _IsSelect = false;
        public bool IsSelect
        {
            get { return _IsSelect; }
            set { SetProperty(ref _IsSelect, value); }
        }

        private bool _IsShareParameter = false;
        public bool IsShareParameter
        {
            get { return _IsShareParameter; }
            set { SetProperty(ref _IsShareParameter, value); }
        }

        //private void ChangedParaKind()
        //{
        //    ParaKind = IsShareParameter ? ParaKindEnum.SharePara : ParaKindEnum.FamilyPara;

        //}

        private Visibility _NodeVisibility = Visibility.Visible;
        public Visibility NodeVisibility
        {
            get { return _NodeVisibility; }
            set { SetProperty(ref _NodeVisibility, value); }
        }

        private bool? _IsApplyError = null;
        public bool? IsApplyError //null表示该对象未应用
        {
            get { return _IsApplyError; }
            set { SetProperty(ref _IsApplyError, value); }
        }

        #endregion


        #region 基本属性

        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value, NameChanged); }
        }
        private void NameChanged()
        {
            IsApplyError = null;
        }

        private ParaKindEnum _ParaKind = ParaKindEnum.FamilyPara;
        public ParaKindEnum ParaKind
        {
            get { return _ParaKind; }
            set { SetProperty(ref _ParaKind, value, ChangedIsFamilyParameter); }
        }

        private void ChangedIsFamilyParameter()
        {
            if (ParaKind == ParaKindEnum.FamilyPara)
            {
                IsShareParameter = false;
                ParaVisibility = null;
                UserModifiable = null;
                HideWhenNoValue = null;
            }
            else
            {
                IsShareParameter = true;
                ParaVisibility = true;
                UserModifiable = true;
                HideWhenNoValue = false;
            }
        }

        private UnitGroup _Discipline = UnitGroup.Common;//该属性无需直接用于Revit参数设置（规程间接作用于参数：UnitGroup规程确定UnitType，间接确定ParameterType）
        public UnitGroup Discipline
        {
            get { return _Discipline; }
            set { SetProperty(ref _Discipline, value, ChangedDiscipline); }
        }
        private void ChangedDiscipline()
        {
            ParaTypeModels.Clear();
            Init(Discipline);
            ParaType = (ParaTypeModels.FirstOrDefault() as ParaTypeAndComboModel).ParaType;
        }

        // DataGrid_参数类型_ComboBox 
        private ObservableCollection<ParaTypeAndComboModel> _ParaTypeModels = new ObservableCollection<ParaTypeAndComboModel>();
        public ObservableCollection<ParaTypeAndComboModel> ParaTypeModels
        {
            get { return _ParaTypeModels; }
            set { SetProperty(ref _ParaTypeModels, value); }
        }

        private ParameterType _ParaType;
        public ParameterType ParaType
        {
            get { return _ParaType; }
            set { SetProperty(ref _ParaType, value, ChangedParaType); }
        }

        private void ChangedParaType()
        {
            string curBuiltInGruop = SysCache.Instance.DiscipToParaTypeToBuiltInGruop[Discipline.ToString()][LabelUtils.GetLabelFor(ParaType)];
            foreach (BuiltInParameterGroup item in SysCache.Instance.EditableBuiltInParaGroup)
            {
                if (LabelUtils.GetLabelFor(item) == curBuiltInGruop)
                {
                    ParaGroup = item;
                }
            }
        }

        private BuiltInParameterGroup _ParaGroup;
        public BuiltInParameterGroup ParaGroup
        {
            get { return _ParaGroup; }
            set { SetProperty(ref _ParaGroup, value); }
        }

        private bool _IsInstancePara = false;
        public bool IsInstancePara
        {
            get { return _IsInstancePara; }
            set { SetProperty(ref _IsInstancePara, value); }
        }

        private string _ParaValue = "";
        public string ParaValue
        {
            get { return _ParaValue; }
            set { SetProperty(ref _ParaValue, value); }
        }

        private string _ParaFormula = "";
        public string ParaFormula
        {
            get { return _ParaFormula; }
            set { SetProperty(ref _ParaFormula, value); }
        }

        #endregion


        #region 附加属性
        private string _Description = "";
        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }

        private bool? _ParaVisibility;
        public bool? ParaVisibility
        {
            get { return _ParaVisibility; }
            set { SetProperty(ref _ParaVisibility, value); }
        }

        private bool? _UserModifiable;
        public bool? UserModifiable
        {
            get { return _UserModifiable; }
            set { SetProperty(ref _UserModifiable, value); }
        }

        private bool? _HideWhenNoValue;
        public bool? HideWhenNoValue
        {
            get { return _HideWhenNoValue; }
            set { SetProperty(ref _HideWhenNoValue, value); }
        }
        #endregion
    }
}
