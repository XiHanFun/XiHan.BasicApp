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
/// id_token 的签名密钥（单例）。
/// </summary>
/// <remarks>
/// 从库里读在用的密钥（私钥解密后放内存，十分钟重读一次），一把都没有就生成一把 RSA 2048 写进库（独立事务）。
/// 签名取最早的在用密钥（几个实例同时生成也只用同一把），JWKS 公布全部在用的。
/// </remarks>
/// <param name="scopes">作用域工厂。</param>
/// <param name="secrets">密钥保护。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="clock">时间源。</param>
public sealed class OAuthSigningKeys(IServiceScopeFactory scopes, ISecretProtector secrets, IIdGenerator<Guid> ids, TimeProvider clock) : IDisposable
{
    /// <summary>
    /// 私钥的加密用途。
    /// </summary>
    public const string Purpose = "XiHan.BasicApp.OpenPlatform.OAuthSigningKey";

    private static readonly TimeSpan _refresh = TimeSpan.FromMinutes(10);
    private readonly SemaphoreSlim _loading = new(1, 1);
    private IReadOnlyList<RsaSecurityKey> _keys = [];
    private DateTimeOffset _loadedAt = DateTimeOffset.MinValue;

    /// <summary>
    /// 签名用的密钥。
    /// </summary>
    public async Task<SigningCredentials> SigningAsync(CancellationToken cancellationToken)
        => new((await KeysAsync(cancellationToken).ConfigureAwait(false))[0], SecurityAlgorithms.RsaSha256);

    /// <summary>
    /// 全部在用的密钥（JWKS）。
    /// </summary>
    public async Task<IReadOnlyList<RsaSecurityKey>> KeysAsync(CancellationToken cancellationToken)
    {
        if (_keys.Count > 0 && clock.GetUtcNow() - _loadedAt < _refresh)
        {
            return _keys;
        }

        await _loading.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_keys.Count > 0 && clock.GetUtcNow() - _loadedAt < _refresh)
            {
                return _keys;
            }

            var keys = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (keys.Count == 0)
            {
                await CreateAsync(cancellationToken).ConfigureAwait(false);
                keys = await LoadAsync(cancellationToken).ConfigureAwait(false);
            }

            if (keys.Count == 0)
            {
                throw new InvalidOperationException("No usable OAuth signing key could be loaded.");
            }

            _keys = keys;
            _loadedAt = clock.GetUtcNow();
            return keys;
        }
        finally
        {
            _loading.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _loading.Dispose();

    private async Task<IReadOnlyList<RsaSecurityKey>> LoadAsync(CancellationToken cancellationToken)
        => await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysOAuthSigningKey, Guid>>()
                .ListAsync(EntityQuery.All<SysOAuthSigningKey>().Where(new Specification<SysOAuthSigningKey>(static k => k.IsActive)), ct).ConfigureAwait(false);
            var keys = new List<RsaSecurityKey>();
            foreach (var row in rows.OrderBy(static k => k.CreatedAt).ThenBy(static k => k.KeyId, StringComparer.Ordinal))
            {
                if (!secrets.TryUnprotect(Purpose, row.PrivateKey, out var pkcs8))
                {
                    continue;
                }

                var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(pkcs8), out _);
                keys.Add(new RsaSecurityKey(rsa) { KeyId = row.KeyId });
            }

            return (IReadOnlyList<RsaSecurityKey>)keys;
        }, cancellationToken).ConfigureAwait(false);

    private async Task CreateAsync(CancellationToken cancellationToken)
    {
        using var rsa = RSA.Create(2048);
        var row = new SysOAuthSigningKey
        {
            Id = ids.NewId(),
            KeyId = OAuthProtocol.Base64UrlEncode(RandomNumberGenerator.GetBytes(12)),
            PrivateKey = secrets.Protect(Purpose, Convert.ToBase64String(rsa.ExportPkcs8PrivateKey())),
            IsActive = true,
        };
        var written = await StoreScope.WriteAsync(scopes, async (services, ct) => await services.GetRequiredService<IRepository<SysOAuthSigningKey, Guid>>().AddAsync(row, ct).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        written.ThrowIfFailure();
    }
}
