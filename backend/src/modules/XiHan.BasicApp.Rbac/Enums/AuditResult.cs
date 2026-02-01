#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditResult
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 审核结果枚举
/// </summary>
public enum AuditResult
{
    /// <summary>
    /// 通过
    /// </summary>
    Pass = 0,

    /// <summary>
    /// 拒绝
    /// </summary>
    Reject = 1,

    /// <summary>
    /// 退回修改
    /// </summary>
    Return = 2
}
