#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestStatus
// Guid:f6a7b8c9-d0e1-2345-1234-678901234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 权限申请状态枚举
/// </summary>
public enum PermissionRequestStatus
{
    /// <summary>
    /// 待审批
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已批准（自动授权完成）
    /// </summary>
    Approved = 1,

    /// <summary>
    /// 已拒绝
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// 已撤回
    /// </summary>
    Withdrawn = 3,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 4
}
