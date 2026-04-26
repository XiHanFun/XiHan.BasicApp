#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysReview.Enum
// Guid:fc01a4ac-0286-4612-a982-c173a9bfa296
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Entities;

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

