#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceAccessLevel
// Guid:a7c2d8e4-3f15-4b92-8d6a-e9f0b1c2d3e4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 资源访问级别枚举
/// 替代原有 IsRequireAuth + IsPublic 两个布尔字段，消除语义矛盾的无效组合
/// </summary>
public enum ResourceAccessLevel
{
    /// <summary>
    /// 匿名可访问（无需登录，无需授权，如健康检查、公开文档）
    /// </summary>
    Public = 0,

    /// <summary>
    /// 仅需认证（登录即可访问，无需具体权限，如个人信息、通知列表）
    /// </summary>
    Authenticated = 1,

    /// <summary>
    /// 需要授权（必须登录且持有对应权限才可访问，默认级别）
    /// </summary>
    Authorized = 2
}
