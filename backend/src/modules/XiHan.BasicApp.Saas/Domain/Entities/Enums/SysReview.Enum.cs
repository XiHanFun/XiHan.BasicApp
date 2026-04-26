#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysReview.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 审核结果枚举
/// </summary>
public enum AuditResult
{
    /// <summary>
    /// 通过
    /// </summary>
    [Description("通过")]
    Pass = 0,

    /// <summary>
    /// 拒绝
    /// </summary>
    [Description("拒绝")]
    Reject = 1,

    /// <summary>
    /// 退回修改
    /// </summary>
    [Description("退回修改")]
    Return = 2
}

/// <summary>
/// 审核状态枚举
/// </summary>
public enum AuditStatus
{
    /// <summary>
    /// 待审核
    /// </summary>
    [Description("待审核")]
    Pending = 0,

    /// <summary>
    /// 审核中
    /// </summary>
    [Description("审核中")]
    InProgress = 1,

    /// <summary>
    /// 审核通过
    /// </summary>
    [Description("审核通过")]
    Approved = 2,

    /// <summary>
    /// 审核拒绝
    /// </summary>
    [Description("审核拒绝")]
    Rejected = 3,

    /// <summary>
    /// 已撤回
    /// </summary>
    [Description("已撤回")]
    Withdrawn = 4
}
