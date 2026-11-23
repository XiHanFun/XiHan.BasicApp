// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Plugins.Domain.Entities;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Plugins.Domain.Packages;

/// <summary>
/// 插件包的信任判定
/// </summary>
/// <remarks>
/// 方案第 7 节：按旁挂清单的发布者与签名里的公钥指纹在 <see cref="PluginsOptions.TrustedPublishers"/> 里找公钥，找到即核对签名，对不上的包一律拒绝
/// 没有签名、或签名的公钥不受信任，只在 <see cref="PluginsOptions.AllowUnsigned"/> 打开时放行
/// 装好之后每次加载前按装时的结论与当时的配置再判一次：撤掉的公钥、关掉的放行开关对已装的版本同样生效
/// </remarks>
/// <param name="options">插件模块选项</param>
public sealed class PluginTrust(PluginsOptions options)
{
    /// <summary>
    /// 判定一个读出来的包
    /// </summary>
    /// <remarks>
    /// 签名与包内容对不上 <c>PLUGINS_VALIDATION_109</c>；不放行时没有签名 <c>PLUGINS_DENIED_102</c>、公钥不受信任 <c>PLUGINS_DENIED_103</c>
    /// </remarks>
    /// <param name="package">插件包</param>
    /// <returns>签名结论</returns>
    public Result<PluginSigner> Check(PluginPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (package.Signature is not { } signature)
        {
            return Admit(package.Descriptor.Publisher, PluginSigner.None);
        }

        if (TrustedKeyOf(package.Descriptor.Publisher, signature.KeyId) is not { } trusted)
        {
            return Admit(package.Descriptor.Publisher, new PluginSigner(PluginSignatureStatus.Untrusted, signature.KeyId));
        }

        using var key = PluginSignature.ImportPublicKey(trusted.PublicKey.Span)!;
        return PluginSignature.Verify(signature.ContentList, signature.Value.Span, key)
            ? new PluginSigner(PluginSignatureStatus.Trusted, signature.KeyId)
            : Error.Validation(PluginsErrors.SignatureInvalid, PluginsMessages.SignatureMismatch, MessageArguments.Of(("key", signature.KeyId)));
    }

    /// <summary>
    /// 加载前按装时的结论再判一次
    /// </summary>
    /// <remarks>
    /// 受信任的版本要求它的公钥还在受信任的发布者里，否则同 <see cref="Check"/> 的放行规则
    /// </remarks>
    /// <param name="publisher">发布者</param>
    /// <param name="signer">装时的签名结论</param>
    public Result Recheck(string? publisher, PluginSigner signer)
    {
        ArgumentNullException.ThrowIfNull(signer);
        var current = signer.Status == PluginSignatureStatus.Trusted && TrustedKeyOf(publisher, signer.KeyId) is null
            ? signer with { Status = PluginSignatureStatus.Untrusted }
            : signer;
        var admitted = Admit(publisher, current);
        return admitted.IsSuccess ? Result.Success() : admitted.Error!;
    }

    private Result<PluginSigner> Admit(string? publisher, PluginSigner signer) => signer.Status switch
    {
        PluginSignatureStatus.Trusted => signer,
        _ when options.AllowUnsigned => signer,
        PluginSignatureStatus.None => Error.Denied(PluginsErrors.PackageUnsigned, PluginsMessages.SignatureMissing),
        _ => Error.Denied(PluginsErrors.PublisherUntrusted, PluginsMessages.SignatureUntrusted, MessageArguments.Of(("key", signer.KeyId), ("publisher", publisher ?? string.Empty))),
    };

    private TrustedPublisher? TrustedKeyOf(string? publisher, string? keyId)
        => publisher is null || keyId is null
            ? null
            : options.TrustedPublishers.FirstOrDefault(p => string.Equals(p.Name, publisher, StringComparison.Ordinal) && string.Equals(p.KeyId, keyId, StringComparison.Ordinal));
}

/// <summary>
/// 插件包的签名结论
/// </summary>
/// <param name="Status">结论</param>
/// <param name="KeyId">签名公钥的指纹；没有签名为空</param>
public sealed record PluginSigner(PluginSignatureStatus Status, string? KeyId)
{
    /// <summary>
    /// 没有签名
    /// </summary>
    public static PluginSigner None { get; } = new(PluginSignatureStatus.None, null);
}
