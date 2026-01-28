#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditStatus
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 4:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 审核状态枚举
/// </summary>
public enum AuditStatus
{
    /// <summary>
    /// 待审核
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 审核中
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// 审核通过
    /// </summary>
    Approved = 2,

    /// <summary>
    /// 审核拒绝
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 已撤回
    /// </summary>
    Withdrawn = 4
}
