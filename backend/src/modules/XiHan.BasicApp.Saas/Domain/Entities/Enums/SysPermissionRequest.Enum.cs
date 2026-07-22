// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限申请状态枚举
/// </summary>
public enum PermissionRequestStatus
{
    /// <summary>
    /// 待审批
    /// </summary>
    [Description("待审批")]
    Pending = 0,

    /// <summary>
    /// 已批准（自动授权完成）
    /// </summary>
    [Description("已批准")]
    Approved = 1,

    /// <summary>
    /// 已拒绝
    /// </summary>
    [Description("已拒绝")]
    Rejected = 2,

    /// <summary>
    /// 已撤回
    /// </summary>
    [Description("已撤回")]
    Withdrawn = 3,

    /// <summary>
    /// 已过期
    /// </summary>
    [Description("已过期")]
    Expired = 4
}
