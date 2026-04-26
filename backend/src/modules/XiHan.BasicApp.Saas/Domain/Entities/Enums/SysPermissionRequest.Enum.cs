#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionRequest.Enum
// Guid:7cfc7b16-59c6-49cf-bab5-82240e3a8460
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    [Description("已批准（自动授权完成）")]
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
