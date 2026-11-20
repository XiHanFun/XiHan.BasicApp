// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Infrastructure.OAuth;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Domain.Services;

/// <summary>
/// id_token 签名密钥的列出、轮换与停用
/// </summary>
/// <remarks>
/// 读写走调用的工作单元；写完丢掉本实例读到的密钥，别的实例按重读间隔跟上（新密钥接替签名之前都来得及）
/// </remarks>
/// <param name="keys">密钥仓储</param>
/// <param name="signing">签名密钥</param>
/// <param name="clock">时间源</param>
public sealed class OAuthSigningKeyService(IRepository<SysOAuthSigningKey, Guid> keys, OAuthSigningKeys signing, TimeProvider clock)
{
    /// <summary>
    /// 全部密钥（含停用的，新的在前）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>密钥</returns>
    public async Task<IReadOnlyList<SysOAuthSigningKey>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await keys.ListAsync(EntityQuery.All<SysOAuthSigningKey>(), cancellationToken).ConfigureAwait(false);
        return [.. rows.OrderByDescending(static k => k.CreatedAt).ThenBy(static k => k.KeyId, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 轮换：生成一把新密钥
    /// </summary>
    /// <remarks>
    /// 新密钥即刻公布，过 <see cref="OAuthSigningKeys.RotationLead"/> 接替签名；还没有在签名的密钥即一建好就签
    /// 已有一把待接替的即 <c>OPENPLATFORM_CONFLICT_004</c>
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新密钥</returns>
    public async Task<Result<SysOAuthSigningKey>> RotateAsync(CancellationToken cancellationToken)
    {
        var active = await ActiveAsync(cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        if (active.FirstOrDefault(k => k.SignsFrom > now) is { } pending)
        {
            return Error.Conflict(OpenPlatformErrors.SigningKeyRotationPending, OpenPlatformMessages.SigningKeyRotationPending, MessageArguments.Of(("keyId", pending.KeyId)));
        }

        var row = signing.NewKey(OAuthSigningKeys.SigningOf(active, now) is null ? null : now + OAuthSigningKeys.RotationLead);
        var added = await keys.AddAsync(row, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        signing.Invalidate();
        return row;
    }

    /// <summary>
    /// 停用一把密钥
    /// </summary>
    /// <remarks>
    /// JWKS 不再公布它，它签过的 id_token 随之验不过；正在签名的即 <c>OPENPLATFORM_CONFLICT_005</c>，已停用的原样返回
    /// </remarks>
    /// <param name="id">密钥行 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停用后的密钥；不存在即 <c>OPENPLATFORM_NOTFOUND_003</c></returns>
    public async Task<Result<SysOAuthSigningKey>> RetireAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await keys.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var key))
        {
            return Error.NotFound(OpenPlatformErrors.SigningKeyNotFound, OpenPlatformMessages.SigningKeyNotFound, MessageArguments.Of(("id", id)));
        }

        if (!key.IsActive)
        {
            return key;
        }

        var now = clock.GetUtcNow();
        if (OAuthSigningKeys.SigningOf(await ActiveAsync(cancellationToken).ConfigureAwait(false), now)?.Id == key.Id)
        {
            return Error.Conflict(OpenPlatformErrors.SigningKeyInUse, OpenPlatformMessages.SigningKeyInUse, MessageArguments.Of(("keyId", key.KeyId)));
        }

        key.IsActive = false;
        key.RetiredAt = now;
        var updated = await keys.UpdateAsync(key, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        signing.Invalidate();
        return key;
    }

    private async Task<IReadOnlyList<SysOAuthSigningKey>> ActiveAsync(CancellationToken cancellationToken)
        => await keys.ListAsync(EntityQuery.All<SysOAuthSigningKey>().Where(new Specification<SysOAuthSigningKey>(static k => k.IsActive)), cancellationToken).ConfigureAwait(false);
}
