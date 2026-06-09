#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResource.Enum
// Guid:edb7a36d-0322-49e5-b417-ccec242549b6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 资源访问级别枚举
/// 替代原有 IsRequireAuth + IsPublic 两个布尔字段，消除语义矛盾的无效组合
/// </summary>
public enum ResourceAccessLevel
{
    /// <summary>
    /// 匿名可访问（无需登录，无需授权，如健康检查、公开文档）
    /// </summary>
    [Description("匿名访问")]
    Public = 0,

    /// <summary>
    /// 仅需认证（登录即可访问，无需具体权限，如个人信息、通知列表）
    /// </summary>
    [Description("仅需认证")]
    Authenticated = 1,

    /// <summary>
    /// 需要授权（必须登录且持有对应权限才可访问，默认级别）
    /// </summary>
    [Description("需要授权")]
    Authorized = 2
}

/// <summary>
/// 资源类型枚举
/// 资源是"被控制对象"（API/数据/文件等），不包含 UI 结构（菜单/按钮在 SysMenu 中维护）
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// API接口资源
    /// </summary>
    [Description("API")]
    Api = 0,

    /// <summary>
    /// 文件资源
    /// </summary>
    [Description("文件")]
    File = 1,

    /// <summary>
    /// 数据表资源
    /// </summary>
    [Description("数据表")]
    DataTable = 2,

    /// <summary>
    /// 业务对象资源
    /// </summary>
    [Description("业务对象")]
    BusinessObject = 3,

    /// <summary>
    /// 其他资源
    /// </summary>
    [Description("其他")]
    Other = 99
}
