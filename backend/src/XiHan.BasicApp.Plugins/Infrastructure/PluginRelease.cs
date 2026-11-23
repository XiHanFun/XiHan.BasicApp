// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.Framework.Primitives.Manifests;
using KernelGrant = XiHan.Framework.Kernel.Plugins.PermissionGrant;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 一个启用的插件应当以什么样子加载
/// </summary>
/// <remarks>
/// 对齐服务从库里读出：当前版本、它的两份清单原文与批准的授予；同一插件的版本或授予变了即要重新加载
/// </remarks>
/// <param name="PluginId">插件 ID</param>
/// <param name="ManifestId">清单 ID</param>
/// <param name="Name">短名</param>
/// <param name="Isolation">隔离级别</param>
/// <param name="VersionId">当前版本 ID</param>
/// <param name="Version">当前版本号</param>
/// <param name="FileId">包文件 ID</param>
/// <param name="Sha256">包的 SHA-256</param>
/// <param name="Signer">上传核对时的签名结论</param>
/// <param name="DescriptorJson">旁挂清单原文</param>
/// <param name="Grants">批准的授予（排序后）</param>
/// <param name="LastErrorCode">库里记的最近一次加载失败的错误码</param>
public sealed record PluginRelease(
    Guid PluginId,
    string ManifestId,
    string Name,
    PluginIsolation Isolation,
    Guid VersionId,
    string Version,
    Guid FileId,
    string Sha256,
    PluginSigner Signer,
    string DescriptorJson,
    IReadOnlyList<string> Grants,
    string? LastErrorCode)
{
    /// <summary>
    /// 交给框架插件宿主的授予
    /// </summary>
    /// <remarks>
    /// 去掉 BasicApp 自己解释的两类：<c>callback:</c>（插件主体持有的宿主权限码）与 <c>data-scope:</c>（插件主体的数据范围）
    /// </remarks>
    public KernelGrant HostGrant => new(Grants.Where(static g => !g.StartsWith(PluginInspector.CallbackPrefix, StringComparison.Ordinal) && !string.Equals(g, PluginInspector.DataScopeAll, StringComparison.Ordinal)));

    /// <summary>
    /// 与另一份是否同一个版本、同一组授予
    /// </summary>
    /// <param name="other">另一份</param>
    public bool SameAs(PluginRelease other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return VersionId == other.VersionId && Grants.SequenceEqual(other.Grants, StringComparer.Ordinal);
    }

    /// <summary>
    /// 旁挂清单
    /// </summary>
    public PluginDescriptor ReadDescriptor()
        => JsonSerializer.Deserialize(DescriptorJson, PluginsJsonContext.Default.PluginDescriptor)
            ?? throw new InvalidOperationException($"The descriptor of plugin '{ManifestId}' version '{Version}' is empty.");

    /// <summary>
    /// 加载成功后交给宿主的运行期贡献
    /// </summary>
    /// <remarks>
    /// 权限码的作用侧与审计取旁挂清单，显示名与说明取插件消息目录里 en-US 的 <c>permissions.&lt;码&gt;.name</c> / <c>.description</c>，没有显示名就用权限码本身
    /// </remarks>
    /// <param name="files">前端文件的原文：页面描述与 10 种区域的文案，按包里的路径</param>
    /// <param name="text">按消息键取 en-US 模板，没有为空</param>
    public LoadedPlugin ToLoaded(IReadOnlyDictionary<string, string> files, Func<string, string?> text)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(text);
        var descriptor = ReadDescriptor();
        return new LoadedPlugin(
            ManifestId,
            Name,
            Version,
            [.. descriptor.Permissions.Select(p => new PermissionDescriptor(p.Code, text(PermissionKey(p.Code, "name")) ?? p.Code, p.Scope, p.Audited, text(PermissionKey(p.Code, "description"))))],
            [.. descriptor.Pages.Select(p => new LoadedPluginPage(p.Code, p.Route, p.TitleKey, p.Icon, p.Order, p.Permission, p.View.Kind, files[p.View.File]))],
            PluginInspector.Locales.ToDictionary(static locale => locale, locale => TextsOf(files[PluginTexts.EntryOf(locale)], locale), StringComparer.Ordinal),
            [.. Grants.Where(static g => g.StartsWith(PluginInspector.CallbackPrefix, StringComparison.Ordinal)).Select(static g => g[PluginInspector.CallbackPrefix.Length..])],
            Grants.Contains(PluginInspector.DataScopeAll, StringComparer.Ordinal));
    }

    /// <summary>
    /// 读一个区域的前端文案
    /// </summary>
    /// <remarks>
    /// 安装时核对过，读不出说明缓存目录被改过
    /// </remarks>
    private IReadOnlyDictionary<string, string> TextsOf(string json, string locale)
        => PluginTexts.TryRead(json, Name, out var texts, out var key)
            ? texts
            : throw new InvalidOperationException($"The front-end texts of plugin '{ManifestId}' version '{Version}' in locale '{locale}' are invalid at '{key}'.");

    /// <summary>
    /// 权限码显示名或说明的消息键
    /// </summary>
    private static string PermissionKey(string code, string part) => "permissions." + code.Replace('-', '_') + "." + part;
}
