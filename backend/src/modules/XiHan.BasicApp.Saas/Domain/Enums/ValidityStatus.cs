#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ValidityStatus
// Guid:bf328e05-2d2e-42e3-87b1-3354d2f5097d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 08:31:07
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
