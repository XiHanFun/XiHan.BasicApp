// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 会话状态枚举（统一表达会话生命周期，替代原 IsOnline/IsRevoked 散落布尔）
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// 活跃（已登录且在线，未撤销、未过期）
    /// </summary>
    [Description("活跃")]
    Active = 0,

    /// <summary>
    /// 离线（用户正常登出或心跳超时下线，但未被强制撤销，仍可按策略重新激活）
    /// </summary>
    [Description("离线")]
    Offline = 1,

    /// <summary>
    /// 已撤销（被管理员/安全策略强制下线，不可恢复，须重新登录）
    /// </summary>
    [Description("已撤销")]
    Revoked = 2,

    /// <summary>
    /// 已过期（超过绝对过期时间 ExpirationTime，自动失效）
    /// </summary>
    [Description("已过期")]
    Expired = 3
}

/// <summary>
/// 设备类型枚举
/// </summary>
public enum DeviceType
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("未知")]
    Unknown = 0,

    /// <summary>
    /// Web浏览器
    /// </summary>
    [Description("Web浏览器")]
    Web = 1,

    /// <summary>
    /// iOS移动端
    /// </summary>
    [Description("iOS移动端")]
    iOS = 2,

    /// <summary>
    /// Android移动端
    /// </summary>
    [Description("Android移动端")]
    Android = 3,

    /// <summary>
    /// Windows桌面
    /// </summary>
    [Description("Windows桌面")]
    Windows = 4,

    /// <summary>
    /// macOS桌面
    /// </summary>
    [Description("macOS桌面")]
    macOS = 5,

    /// <summary>
    /// Linux桌面
    /// </summary>
    [Description("Linux桌面")]
    Linux = 6,

    /// <summary>
    /// 平板设备
    /// </summary>
    [Description("平板设备")]
    Tablet = 7,

    /// <summary>
    /// 小程序
    /// </summary>
    [Description("小程序")]
    MiniProgram = 8,

    /// <summary>
    /// API调用
    /// </summary>
    [Description("API调用")]
    Api = 9
}
