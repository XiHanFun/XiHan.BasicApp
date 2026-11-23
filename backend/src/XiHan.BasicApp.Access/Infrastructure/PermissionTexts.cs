// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 权限显示名、说明与资源显示名的区域文案
/// </summary>
/// <remarks>
/// 约定键 <c>permissions.&lt;权限码&gt;.name</c> / <c>.description</c>（权限码的 <c>-</c> 换 <c>_</c>，与框架 OpenAPI 生成器读的是同一套键），在所属包的语言文件里，按调用方的区域取
/// 每个码在 10 种区域都有显示名与说明（<c>PackageStructureTests</c> 守着）；回退链用尽仍没有时才用描述表里的英文
/// 资源显示名取接口文档的资源标签 <c>openapi.tags.&lt;资源&gt;.name</c>：内建权限码的资源都登记了标签（<c>PackageStructureTests</c> 守着），插件的在它握手带来的消息目录里
/// </remarks>
public static class PermissionTexts
{
    /// <summary>
    /// 调用方区域的显示名
    /// </summary>
    /// <param name="permission">权限描述</param>
    /// <param name="context">当前调用</param>
    /// <returns>显示名</returns>
    public static string NameOf(PermissionDescriptor permission, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(permission);
        return Resolve(permission.Code, "name", context) ?? permission.DisplayName;
    }

    /// <summary>
    /// 调用方区域的说明
    /// </summary>
    /// <param name="permission">权限描述</param>
    /// <param name="context">当前调用</param>
    /// <returns>说明；描述表里也没有时为空</returns>
    public static string? DescriptionOf(PermissionDescriptor permission, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(permission);
        return Resolve(permission.Code, "description", context) ?? permission.Description;
    }

    /// <summary>
    /// 调用方区域的资源显示名
    /// </summary>
    /// <param name="permission">权限描述</param>
    /// <param name="context">当前调用</param>
    /// <returns>资源显示名；资源没有标签（没有操作的插件资源）时为资源段本身</returns>
    public static string ResourceNameOf(PermissionDescriptor permission, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(permission);
        return Find("openapi.tags." + permission.Resource.Replace('-', '_') + ".name", context) ?? permission.Resource;
    }

    private static string? Resolve(string code, string part, InvocationContext context)
        => Find("permissions." + code.Replace('-', '_') + "." + part, context);

    private static string? Find(string key, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var localizer = context.Services.GetService<IMessageLocalizer>();
        var locale = localizer?.Options.Resolve(context);
        return localizer is not null && locale is not null && localizer.Resolve(key, locale, null).TryGetValue(out var text) ? text.Text : null;
    }
}
