#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasOpenApiSecurityClientStore
// Guid:7c40e1b8-2f96-4a53-8d17-9b6e3c0a5f2d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Web.Api.Security.OpenApi;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// SaaS OpenAPI 安全客户端存储（数据库凭证 + 配置客户端复合实现，覆盖框架默认配置源实现）
/// </summary>
/// <remarks>
/// 框架 <see cref="XiHanOpenApiSecurityMiddleware"/> 通过本存储按 AccessKey 查客户端，解密还原明文密钥后参与
/// HMAC/RSA/SM2 验签、内容签名、加解密与防重放。查找顺序：
/// <list type="number">
/// <item>数据库凭证 <see cref="SysUserApiCredential"/>（第三方服务端 AppKey/AppSecret，密钥可逆加密落库）；</item>
/// <item>未命中时回退 appsettings 的 <c>Clients</c> 静态客户端（第一方前端整体签名等固定接入方）。</item>
/// </list>
/// 框架以 <c>TryAddScoped</c> 注册默认实现（仅读配置），故此处须 <c>Replace</c> 覆盖，否则 DB 凭证永不生效。
/// fail-closed：查不到 / 已停用 / 已过期 / 解密失败一律拒绝。
/// </remarks>
public sealed class SaasOpenApiSecurityClientStore : IOpenApiSecurityClientStore
{
    private readonly IUserApiCredentialRepository _credentialRepository;
    private readonly IUserApiCredentialSecretProtector _secretProtector;
    private readonly IOptionsMonitor<XiHanOpenApiSecurityOptions> _optionsMonitor;
    private readonly ILogger<SaasOpenApiSecurityClientStore> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasOpenApiSecurityClientStore(
        IUserApiCredentialRepository credentialRepository,
        IUserApiCredentialSecretProtector secretProtector,
        IOptionsMonitor<XiHanOpenApiSecurityOptions> optionsMonitor,
        ILogger<SaasOpenApiSecurityClientStore> logger)
    {
        _credentialRepository = credentialRepository;
        _secretProtector = secretProtector;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OpenApiSecurityClient?> FindByAccessKeyAsync(string accessKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
        {
            return null;
        }

        var normalizedAccessKey = accessKey.Trim();

        var credential = await _credentialRepository.GetByAppKeyAsync(normalizedAccessKey, cancellationToken);
        if (credential is not null)
        {
            return MapFromCredential(credential);
        }

        // 数据库无此 AppKey：回退配置客户端（第一方固定接入方）
        return MapFromConfiguredClient(normalizedAccessKey);
    }

    /// <summary>
    /// 数据库凭证 → 安全客户端（解密密钥；停用/过期标记 IsEnabled=false 由中间件统一拒绝）
    /// </summary>
    private OpenApiSecurityClient? MapFromCredential(SysUserApiCredential credential)
    {
        string? secretKey;
        try
        {
            secretKey = _secretProtector.Unprotect(credential.SecretCipher);
        }
        catch (Exception ex)
        {
            // 解密失败即拒绝（fail-closed，不回退明文）
            _logger.LogWarning(ex, "OpenAPI 凭证 [{AppKey}] 密钥解密失败，已拒绝。", credential.AppKey);
            return null;
        }

        if (string.IsNullOrEmpty(secretKey))
        {
            return null;
        }

        var isActive = credential.Status == EnableStatus.Enabled
            && (!credential.ExpirationTime.HasValue || credential.ExpirationTime.Value > DateTimeOffset.UtcNow);

        return new OpenApiSecurityClient
        {
            AccessKey = credential.AppKey,
            SecretKey = secretKey,
            SignatureAlgorithm = MapSignatureAlgorithm(credential.SignatureAlgorithm),
            IpWhitelist = SplitWhitelist(credential.IpWhitelist),
            IsEnabled = isActive,
            // 凭证归属用户：供开放接口日志记录"是谁的密钥发起的调用"
            OwnerUserId = credential.UserId
        };
    }

    /// <summary>
    /// 配置客户端 → 安全客户端（与框架默认存储同口径；供第一方固定接入方使用）
    /// </summary>
    private OpenApiSecurityClient? MapFromConfiguredClient(string accessKey)
    {
        var configured = _optionsMonitor.CurrentValue.Clients.FirstOrDefault(item =>
            string.Equals(item.AccessKey, accessKey, StringComparison.OrdinalIgnoreCase));
        if (configured is null)
        {
            return null;
        }

        return new OpenApiSecurityClient
        {
            AccessKey = configured.AccessKey,
            SecretKey = configured.SecretKey,
            EncryptKey = configured.EncryptKey,
            PublicKey = configured.PublicKey,
            Sm2PublicKey = configured.Sm2PublicKey,
            SignatureAlgorithm = configured.SignatureAlgorithm,
            ContentSignatureAlgorithm = configured.ContentSignatureAlgorithm,
            EncryptionAlgorithm = configured.EncryptionAlgorithm,
            AllowResponseEncryption = configured.AllowResponseEncryption,
            IpWhitelist = [.. configured.IpWhitelist],
            IsEnabled = configured.IsEnabled
        };
    }

    /// <summary>
    /// 凭证签名算法枚举 → 框架算法标识；未覆盖项返回 null（回退中间件全局默认）
    /// </summary>
    private static string? MapSignatureAlgorithm(SignatureType type)
    {
        return type switch
        {
            SignatureType.HmacSha256 => "HMACSHA256",
            SignatureType.HmacSha512 => "HMACSHA512",
            SignatureType.RsaSha256 => "RSASHA256",
            SignatureType.Sm2 => "SM2",
            _ => null
        };
    }

    /// <summary>
    /// 逗号/分号/竖线分隔的 IP 白名单串 → 规则集合（空串表示不限制）
    /// </summary>
    private static IReadOnlyCollection<string> SplitWhitelist(string? raw)
    {
        return string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
