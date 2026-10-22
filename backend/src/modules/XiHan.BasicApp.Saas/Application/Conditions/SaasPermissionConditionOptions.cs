// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 权限条件的生效模式
/// </summary>
public enum PermissionConditionMode
{
    /// <summary>
    /// 不求值：有条件的授予按无条件授予、有条件的禁止按无条件禁止，只作应急
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// 照常求值但不拦截：本该拒绝的请求只记下来
    /// </summary>
    LogOnly = 1,

    /// <summary>
    /// 条件不满足即拒绝
    /// </summary>
    Enforce = 2
}

/// <summary>
/// 权限条件选项
/// </summary>
/// <remarks>
/// 绑定配置节 <see cref="SectionName"/>，经 <c>IOptionsMonitor</c> 读取，改了热生效。
/// </remarks>
public sealed class SaasPermissionConditionOptions
{
    /// <summary>
    /// 配置节名
    /// </summary>
    public const string SectionName = "Saas:PermissionConditions";

    /// <summary>
    /// 生效模式，缺省 <see cref="PermissionConditionMode.Enforce"/>
    /// </summary>
    public PermissionConditionMode Mode { get; set; } = PermissionConditionMode.Enforce;
}
