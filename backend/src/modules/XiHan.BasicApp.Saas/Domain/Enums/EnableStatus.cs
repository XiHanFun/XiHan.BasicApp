using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 启用状态
/// </summary>
public enum EnableStatus
{
    /// <summary>
    /// 禁用
    /// </summary>
    [Description("禁用")]
    Disabled = 0,

    /// <summary>
    /// 启用
    /// </summary>
    [Description("启用")]
    Enabled = 1
}
