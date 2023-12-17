using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    public enum ImportOrExportEnum
    {
        [Help("导入/导出", IsHidden = true)]
        ImportOrExport,

        [Help("导入Excel")]
        ImportOfExcel,

        [Help("导出Excel")]
        ExportToExcel,

        [Help("导入共享参数")]
        ImportOfShareParameter,

        [Help("导出共享参数")]
        ExportOfShareParameter
    }

    public enum BatchActionEnum
    {
        [Help("批量操作", IsHidden = true)]
        BatchAction,

        [Help("复制属性")]
        CopyProperty,

        [Help("删除属性")]
        DeleteProperty
    }

    public enum AdditionPropertyEnum
    {
        [Help("附加属性", IsHidden = true)]
        AdditionProperty,

        [Help("可见性")]
        ParaVisibility,

        [Help("用户可编辑")]
        UserModifiable,

        [Help("参数说明")]
        Description,

        [Help("无值时隐藏")]
        HideWhenNoValue
    }

    public enum ParaKindEnum
    {
        [Help("所有种类", IsHidden = true)]
        AllKind,

        [Help("全局参数")]
        GlobalPara,

        [Help("项目参数")]
        ProjectPara,

        [Help("共享参数")]
        SharePara,

        [Help("族参数")]
        FamilyPara,
    }

    public enum DisciplineEnum
    {
        [Help("公共")]
        Common,

        [Help("结构")]
        Structural,

        [Help("HVAC")]
        HVAC,

        [Help("电气")]
        Electrical,

        [Help("管道")]
        Piping,

        [Help("能源")]
        Energy,
    }

    public class ComboModelBase : BindableBase
    {
        public string Name { get; set; }

        private bool _IsHidden = false;
        public bool IsHidden
        {
            get { return _IsHidden; }
            set { SetProperty(ref _IsHidden, value); }
        }


        private bool _IsSelect = false;
        public bool IsSelect
        {
            get { return _IsSelect; }
            set { SetProperty(ref _IsSelect, value); }
        }
    }
}
