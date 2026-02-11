#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ViolationAction
// Guid:75e42411-6e5b-4263-a94a-5b3bfcd4d293
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 违规处理方式枚举
/// </summary>
public enum ViolationAction
{
    /// <summary>
    /// 拒绝操作
    /// </summary>
    Deny = 0,

    /// <summary>
    /// 警告但允许
    /// </summary>
    Warning = 1,

    /// <summary>
    /// 仅记录日志
    /// </summary>
    Log = 2,

    /// <summary>
    /// 需要审批
    /// </summary>
    RequireApproval = 3
}
