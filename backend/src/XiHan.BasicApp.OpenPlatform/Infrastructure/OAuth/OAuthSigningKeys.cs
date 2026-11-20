// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.OpenPlatform.Infrastructure.OAuth;

/// <summary>
/// id_token 的签名密钥（单例）
/// </summary>
/// <remarks>
/// 从库里读在用的密钥（私钥解密后放内存，十分钟重读一次），一把都没有就生成一把 RSA 2048 写进库（独立事务）
/// 签名取 <see cref="SigningOf"/> 选出的那把，JWKS 公布全部在用的；有密钥在上次读取之后到了签名时间也重读，接替前先确认它没被停用
/// </remarks>
/// <param name="scopes">作用域工厂</param>
/// <param name="secrets">密钥保护</param>
/// <param name="ids">标识生成器</param>
/// <param name="clock">时间源</param>
public sealed class OAuthSigningKeys(IServiceScopeFactory scopes, ISecretProtector secrets, IIdGenerator<Guid> ids, TimeProvider clock) : IDisposable
{
    /// <summary>
    /// 私钥的加密用途
    /// </summary>
    public const string Purpose = "XiHan.BasicApp.OpenPlatform.OAuthSigningKey";

    private static readonly TimeSpan _refresh = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 轮换出的新密钥从公布到接替签名的间隔
    /// </summary>
    /// <remarks>
    /// 各实例最晚十分钟重读一次、JWKS 响应最多缓存五分钟：过了这段时间，各实例与依赖方都拿得到新公钥
    /// </remarks>
    public static readonly TimeSpan RotationLead = _refresh + TimeSpan.FromSeconds(OAuthEndpoints.MetadataMaxAgeSeconds);

    private readonly SemaphoreSlim _loading = new(1, 1);
    private volatile Snapshot? _snapshot;

    /// <summary>
    /// 此刻签名的密钥
    /// </summary>
    /// <remarks>
    /// 在用、已到签名时间的密钥里取签名时间最晚的（空即一建好就签，排最前；同一时刻的取最早建的）；没有即空
    /// </remarks>
    /// <param name="keys">密钥</param>
    /// <param name="now">此刻</param>
    public static SysOAuthSigningKey? SigningOf(IEnumerable<SysOAuthSigningKey> keys, DateTimeOffset now)
        => keys.Where(k => k.IsActive && (k.SignsFrom is null || k.SignsFrom <= now))
            .OrderByDescending(static k => k.SignsFrom ?? DateTimeOffset.MinValue)
            .ThenBy(static k => k.CreatedAt)
            .ThenBy(static k => k.KeyId, StringComparer.Ordinal)
            .FirstOrDefault();

    /// <summary>
    /// 签名用的密钥
    /// </summary>
    public async Task<SigningCredentials> SigningAsync(CancellationToken cancellationToken)
    {
        var snapshot = await SnapshotAsync(cancellationToken).ConfigureAwait(false);
        var signing = SigningOf(snapshot.Rows, clock.GetUtcNow()) ?? throw new InvalidOperationException("No OAuth signing key is due to sign.");
        return new SigningCredentials(snapshot.Keys[signing.KeyId], SecurityAlgorithms.RsaSha256);
    }

    /// <summary>
    /// 全部在用的密钥（JWKS）
    /// </summary>
    public async Task<IReadOnlyList<RsaSecurityKey>> KeysAsync(CancellationToken cancellationToken)
        => (await SnapshotAsync(cancellationToken).ConfigureAwait(false)).Published;

    /// <summary>
    /// 生成一把新密钥（未写库）
    /// </summary>
    /// <param name="signsFrom">开始签名的时间；空即一建好就签</param>
    public SysOAuthSigningKey NewKey(DateTimeOffset? signsFrom)
    {
        using var rsa = RSA.Create(2048);
        return new SysOAuthSigningKey
        {
            Id = ids.NewId(),
            KeyId = OAuthProtocol.Base64UrlEncode(RandomNumberGenerator.GetBytes(12)),
            PrivateKey = secrets.Protect(Purpose, Convert.ToBase64String(rsa.ExportPkcs8PrivateKey())),
            IsActive = true,
            SignsFrom = signsFrom,
        };
    }

    /// <summary>
    /// 丢掉本实例读到的密钥，下次用到时重读
    /// </summary>
    /// <remarks>
    /// 轮换与停用后调用；别的实例按重读间隔跟上
    /// </remarks>
    public void Invalidate() => _snapshot = null;

    /// <inheritdoc />
    public void Dispose() => _loading.Dispose();

    private bool IsFresh(Snapshot snapshot)
    {
        var now = clock.GetUtcNow();
        return now - snapshot.LoadedAt < _refresh && !snapshot.Rows.Any(k => k.SignsFrom > snapshot.LoadedAt && k.SignsFrom <= now);
    }

    private async Task<Snapshot> SnapshotAsync(CancellationToken cancellationToken)
    {
        var current = _snapshot;
        if (current is not null && IsFresh(current))
        {
            return current;
        }

        await _loading.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            current = _snapshot;
            if (current is not null && IsFresh(current))
            {
                return current;
            }

            var loaded = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (loaded.Rows.Count == 0)
            {
                await CreateAsync(cancellationToken).ConfigureAwait(false);
                loaded = await LoadAsync(cancellationToken).ConfigureAwait(false);
            }

            if (loaded.Rows.Count == 0)
            {
                throw new InvalidOperationException("No usable OAuth signing key could be loaded.");
            }

            _snapshot = loaded;
            return loaded;
        }
        finally
        {
            _loading.Release();
        }
    }

    private async Task<Snapshot> LoadAsync(CancellationToken cancellationToken)
    {
        var loadedAt = clock.GetUtcNow();
        return await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysOAuthSigningKey, Guid>>()
                .ListAsync(EntityQuery.All<SysOAuthSigningKey>().Where(new Specification<SysOAuthSigningKey>(static k => k.IsActive)), ct).ConfigureAwait(false);
            var usable = new List<SysOAuthSigningKey>();
            var keys = new Dictionary<string, RsaSecurityKey>(StringComparer.Ordinal);
            foreach (var row in rows.OrderBy(static k => k.CreatedAt).ThenBy(static k => k.KeyId, StringComparer.Ordinal))
            {
                if (!secrets.TryUnprotect(Purpose, row.PrivateKey, out var pkcs8))
                {
                    continue;
                }

                var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(pkcs8), out _);
                usable.Add(row);
                keys[row.KeyId] = new RsaSecurityKey(rsa) { KeyId = row.KeyId };
            }

            return new Snapshot(usable, keys, [.. usable.Select(r => keys[r.KeyId])], loadedAt);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task CreateAsync(CancellationToken cancellationToken)
    {
        var row = NewKey(null);
        var written = await StoreScope.WriteAsync(scopes, async (services, ct) => await services.GetRequiredService<IRepository<SysOAuthSigningKey, Guid>>().AddAsync(row, ct).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        written.ThrowIfFailure();
    }

    /// <summary>
    /// 一次读到的在用密钥
    /// </summary>
    /// <param name="Rows">解得开的密钥行（按建好先后）</param>
    /// <param name="Keys">按 <c>kid</c> 的私钥</param>
    /// <param name="Published">JWKS 公布的密钥（按建好先后）</param>
    /// <param name="LoadedAt">开始读的时刻</param>
    private sealed record Snapshot(IReadOnlyList<SysOAuthSigningKey> Rows, IReadOnlyDictionary<string, RsaSecurityKey> Keys, IReadOnlyList<RsaSecurityKey> Published, DateTimeOffset LoadedAt);
}
