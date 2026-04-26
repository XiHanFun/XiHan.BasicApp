using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 有效性状态（用于关联关系：角色权限、用户角色、用户部门等）
/// </summary>
public enum ValidityStatus
{
    /// <summary>
    /// 无效
    /// </summary>
    [Description("无效")]
    Invalid = 0,

    /// <summary>
    /// 有效
    /// </summary>
    [Description("有效")]
    Valid = 1
}
