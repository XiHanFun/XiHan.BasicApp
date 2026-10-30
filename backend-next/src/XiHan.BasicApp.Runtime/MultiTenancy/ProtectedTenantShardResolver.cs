// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.MultiTenancy;

/// <summary>
/// 解密租户连接串的分片解析器。
/// </summary>
/// <remarks>
/// 包着框架的 <see cref="TenantStoreShardResolver"/>：租户目录里的连接串是 <see cref="ISecretProtector"/> 的密文（方案 5.10，框架原样读出），
/// 取到分片后按 <see cref="Purpose"/> 解密再交给持久化。解不开（密钥换了、被篡改）是基础设施故障，直接失败、不降级为共库。
/// </remarks>
/// <param name="inner">框架的分片解析器。</param>
/// <param name="protector">机密保护器。</param>
public sealed class ProtectedTenantShardResolver(TenantStoreShardResolver inner, ISecretProtector protector) : ITenantShardResolver
{
    /// <summary>
    /// 连接串加密的用途。
    /// </summary>
    public const string Purpose = "XiHan.BasicApp.Tenancy.ConnectionString";

    /// <inheritdoc />
    public async ValueTask<Result<Maybe<TenantShard>>> ResolveAsync(string tenantId, CancellationToken cancellationToken)
    {
        var resolved = await inner.ResolveAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var shard) || !shard.TryGetValue(out var value))
        {
            return resolved;
        }

        if (!protector.TryUnprotect(Purpose, value.ConnectionString, out var plaintext))
        {
            return Error.Literal(RuntimeErrors.ShardSecretUnreadable, $"The connection string of tenant '{tenantId}' cannot be decrypted.");
        }

        return Maybe.Some(new TenantShard(value.TenantId, plaintext, value.Provider, value.Properties));
    }

    /// <inheritdoc />
    public void Invalidate(string? tenantId = null) => inner.Invalidate(tenantId);
}
