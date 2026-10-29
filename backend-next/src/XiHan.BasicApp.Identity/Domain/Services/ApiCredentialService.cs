// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 个人的开放接口凭证（4.x 个人中心「开放接口」）：每人最多 5 个；AppKey 为 <c>ak_</c> + 24 位、密钥为 <c>sk_</c> + 43 位字母数字，密钥加密存、只在创建与滚动时返回一次。
/// 签名算法与 IP 白名单在账号安全上，作用于本人全部凭证（4.x 页面上的「调用安全设置」存进了用户设置、验签不读，这里改为真正生效）。
/// </summary>
/// <param name="credentials">凭证仓储。</param>
/// <param name="secrets">密钥保护。</param>
/// <param name="ids">标识生成器。</param>
public sealed class ApiCredentialService(IRepository<SysUserApiCredential, Guid> credentials, ISecretProtector secrets, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 每人的凭证上限（4.x 同）。
    /// </summary>
    public const int Limit = 5;

    /// <summary>
    /// 密钥的加密用途。
    /// </summary>
    public const string SecretPurpose = "XiHan.BasicApp.Identity.ApiCredentialSecret";

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// 本人的全部凭证（按创建先后）。
    /// </summary>
    /// <param name="userId">用户。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>凭证。</returns>
    public async Task<IReadOnlyList<SysUserApiCredential>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await credentials.ListAsync(EntityQuery.All<SysUserApiCredential>().Where(new Specification<SysUserApiCredential>(c => c.UserId == userId)), cancellationToken).ConfigureAwait(false);
        return [.. rows.OrderBy(static c => c.CreatedAt).ThenBy(static c => c.AppKey, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 取本人的一个凭证；别人的与不存在的一样是 <c>IDENTITY_NOTFOUND_003</c>。
    /// </summary>
    /// <param name="userId">用户。</param>
    /// <param name="id">凭证 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>凭证。</returns>
    public async Task<Result<SysUserApiCredential>> FindOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var found = await credentials.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var credential) && credential.UserId == userId
            ? credential
            : Error.NotFound(IdentityErrors.ApiCredentialNotFound, IdentityMessages.ApiCredentialNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 新建凭证：到了上限即 <c>IDENTITY_CONFLICT_007</c>。
    /// </summary>
    /// <param name="userId">用户。</param>
    /// <param name="name">名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>凭证与密钥明文。</returns>
    public async Task<Result<(SysUserApiCredential Credential, string Secret)>> CreateAsync(Guid userId, string name, CancellationToken cancellationToken)
    {
        var existing = await ListAsync(userId, cancellationToken).ConfigureAwait(false);
        if (existing.Count >= Limit)
        {
            return Error.Conflict(IdentityErrors.ApiCredentialLimitReached, IdentityMessages.ApiCredentialLimitReached, MessageArguments.Of(("max", Limit)));
        }

        var appKey = await NewAppKeyAsync(cancellationToken).ConfigureAwait(false);
        var secret = NewSecret();
        var credential = new SysUserApiCredential
        {
            Id = ids.NewId(),
            UserId = userId,
            Name = name,
            AppKey = appKey,
            SecretCipher = secrets.Protect(SecretPurpose, secret),
            IsEnabled = true,
        };
        var added = await credentials.AddAsync(credential, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : (credential, secret);
    }

    /// <summary>
    /// 滚动密钥：旧密钥立即失效。
    /// </summary>
    /// <param name="credential">凭证。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新密钥明文。</returns>
    public async Task<Result<string>> RotateAsync(SysUserApiCredential credential, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credential);
        var secret = NewSecret();
        credential.SecretCipher = secrets.Protect(SecretPurpose, secret);
        var updated = await credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : secret;
    }

    /// <summary>
    /// 启用或停用。
    /// </summary>
    /// <param name="credential">凭证。</param>
    /// <param name="enabled">是否启用。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> SetEnabledAsync(SysUserApiCredential credential, bool enabled, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credential);
        credential.IsEnabled = enabled;
        return await credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 批量删除本人的凭证（软删除，AppKey 立即不能用）；别人的逐项 <c>IDENTITY_NOTFOUND_003</c>。
    /// </summary>
    /// <param name="userId">用户。</param>
    /// <param name="credentialIds">凭证 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> DeleteAsync(Guid userId, IReadOnlyList<Guid> credentialIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credentialIds);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in credentialIds.Distinct())
        {
            var found = await FindOwnedAsync(userId, id, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var credential))
            {
                failed.Add(new BatchFailure(id, found.Error!.Code));
            }
            else if ((await credentials.RemoveAsync(credential, cancellationToken).ConfigureAwait(false)) is { IsFailure: true } removed)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
            }
            else
            {
                succeeded.Add(id);
            }
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 写调用安全设置：算法须是 <see cref="ApiSignatureAlgorithms.All"/> 之一（<c>IDENTITY_VALIDATION_012</c>），白名单每条须是 IP 或 CIDR（<c>IDENTITY_VALIDATION_013</c>）。
    /// 不在这里保存，由调用方连同账号安全一起更新。
    /// </summary>
    /// <param name="security">账号安全。</param>
    /// <param name="algorithm">签名算法。</param>
    /// <param name="ipAllowList">白名单。</param>
    /// <returns>结果。</returns>
    public static Result ApplySettings(SysUserSecurity security, string? algorithm, IReadOnlyList<string>? ipAllowList)
    {
        ArgumentNullException.ThrowIfNull(security);
        if (ApiSignatureAlgorithms.Normalize(algorithm) is not { } normalized)
        {
            return Error.Validation(IdentityErrors.ApiSignatureAlgorithmInvalid, IdentityMessages.ApiSignatureAlgorithmInvalid, MessageArguments.Of(("value", algorithm ?? string.Empty)));
        }

        if (!ApiIpAllowList.TryNormalize(string.Join('\n', ipAllowList ?? []), out var entries, out var invalid))
        {
            return Error.Validation(IdentityErrors.ApiIpAllowListInvalid, IdentityMessages.ApiIpAllowListInvalid, MessageArguments.Of(("value", invalid)));
        }

        if (entries.Count > ApiIpAllowList.MaxEntries)
        {
            return Error.Validation(IdentityErrors.ApiIpAllowListTooLong, IdentityMessages.ApiIpAllowListTooLong, MessageArguments.Of(("max", ApiIpAllowList.MaxEntries)));
        }

        security.ApiSignatureAlgorithm = normalized == ApiSignatureAlgorithms.HmacSha256 ? null : normalized;
        security.ApiIpAllowList = entries.Count == 0 ? null : string.Join('\n', entries);
        return Result.Success();
    }

    /// <summary>
    /// 账号安全上的签名算法（没设即 HMAC-SHA256）。
    /// </summary>
    /// <param name="security">账号安全。</param>
    /// <returns>算法。</returns>
    public static string AlgorithmOf(SysUserSecurity security)
        => ApiSignatureAlgorithms.Normalize(security?.ApiSignatureAlgorithm) ?? ApiSignatureAlgorithms.HmacSha256;

    /// <summary>
    /// 账号安全上的 IP 白名单。
    /// </summary>
    /// <param name="security">账号安全。</param>
    /// <returns>各条。</returns>
    public static IReadOnlyList<string> AllowListOf(SysUserSecurity security) => ApiIpAllowList.Split(security?.ApiIpAllowList);

    private async Task<string> NewAppKeyAsync(CancellationToken cancellationToken)
    {
        // 24 位字母数字约 142 比特，撞上的概率可以忽略；仍按 4.x 查一次、最多重试 3 次
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var appKey = "ak_" + RandomNumberGenerator.GetString(Alphabet, 24);
            if (await credentials.CountAsync(EntityQuery.All<SysUserApiCredential>().Where(new Specification<SysUserApiCredential>(c => c.AppKey == appKey)), cancellationToken).ConfigureAwait(false) == 0)
            {
                return appKey;
            }
        }

        throw new InvalidOperationException("Could not generate a unique AppKey.");
    }

    private static string NewSecret() => "sk_" + RandomNumberGenerator.GetString(Alphabet, 43);
}
