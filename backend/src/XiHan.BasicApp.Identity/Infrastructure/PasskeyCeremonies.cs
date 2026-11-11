// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.WebAuthn;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 通行密钥的登记与登录仪式
/// </summary>
/// <remarks>
/// 作用域服务。选项与核对用框架 <c>Security.WebAuthn</c>（宿主配了 <c>XiHan:Security:WebAuthn</c> 才装），这里管挑战的发放与回收、凭据的读取
/// 每次仪式发一张一次性票据，挑战存在共享缓存里、按选项的超时过期；回收靠删除的返回值判定，同一张票据只有一次能换出挑战
/// 用户句柄是用户 ID 的 16 个字节，不含个人信息
/// </remarks>
/// <param name="cache">共享缓存（挑战）</param>
/// <param name="passkeys">通行密钥仓储</param>
/// <param name="services">作用域服务（框架的仪式没装即通行密钥不可用）</param>
public sealed class PasskeyCeremonies(ISharedCache cache, IRepository<SysUserPasskey, Guid> passkeys, IServiceProvider services)
{
    /// <summary>
    /// 每人最多登记的通行密钥数
    /// </summary>
    public const int MaxPerUser = 10;

    private const string RegistrationPrefix = "identity:passkey:registration:";
    private const string AuthenticationPrefix = "identity:passkey:authentication:";

    /// <summary>
    /// 通行密钥是否可用：宿主配了、登录方式也开放
    /// </summary>
    /// <param name="context">调用上下文</param>
    /// <returns>框架的仪式；宿主没配即 <c>IDENTITY_UNAVAILABLE_007</c>，系统参数没开放即 <c>IDENTITY_DENIED_016</c></returns>
    public async ValueTask<Result<WebAuthnCeremonies>> EnsureOpenAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (services.GetService<WebAuthnCeremonies>() is not { } ceremonies)
        {
            return Error.Unavailable(IdentityErrors.PasskeysNotConfigured, IdentityMessages.PasskeysNotConfigured);
        }

        var methods = await LoginMethods.ReadAsync(context).ConfigureAwait(false);
        if (!methods.TryGetValue(out var open))
        {
            return methods.Error!;
        }

        return open.Contains(LoginMethods.Passkey)
            ? ceremonies
            : Error.Denied(IdentityErrors.LoginMethodDisabled, IdentityMessages.LoginMethodDisabled, MessageArguments.Of(("method", LoginMethods.Passkey)));
    }

    /// <summary>
    /// 本人登记的全部通行密钥
    /// </summary>
    /// <param name="userId">用户</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按登记时间排序</returns>
    public async Task<IReadOnlyList<SysUserPasskey>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await passkeys.ListAsync(EntityQuery.All<SysUserPasskey>().Where(new Specification<SysUserPasskey>(p => p.UserId == userId)), cancellationToken).ConfigureAwait(false);
        return [.. rows.OrderBy(static p => p.CreatedAt)];
    }

    /// <summary>
    /// 按凭据 ID 找通行密钥
    /// </summary>
    /// <param name="credentialId">凭据 ID（base64url）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<SysUserPasskey?> FindAsync(string credentialId, CancellationToken cancellationToken)
    {
        var rows = await passkeys.ListAsync(EntityQuery.All<SysUserPasskey>().Where(new Specification<SysUserPasskey>(p => p.CredentialId == credentialId)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? null : rows[0];
    }

    /// <summary>
    /// 发起登记：生成创建选项，挑战记在票据上
    /// </summary>
    /// <param name="ceremonies">框架的仪式</param>
    /// <param name="user">登记的人</param>
    /// <param name="registered">已登记的通行密钥（认证器上已有的不再重复登记）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<(string Ticket, WebAuthnCreationOptions Options)> BeginRegistrationAsync(WebAuthnCeremonies ceremonies, SysUser user, IReadOnlyList<SysUserPasskey> registered, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ceremonies);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(registered);
        // 认证器的账号选择器里只显示名字：租户账号带上注册地，同名的账号才分得开
        var name = string.Equals(user.HomeTenantId, TenantIds.Platform, StringComparison.Ordinal) ? user.UserName : user.UserName + "@" + user.HomeTenantId;
        var entity = new WebAuthnUserEntity(UserHandleOf(user.Id), name, string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName);
        var options = ceremonies.CreateRegistrationOptions(entity, [.. registered.Select(DescriptorOf)]);
        var ticket = RefreshTokens.Generate();
        await cache.SetAsync(new CacheKey(RegistrationPrefix + Hash(ticket)), user.Id.ToString("N") + "|" + options.Challenge, CacheEntryOptions.Expires(ceremonies.Options.Timeout), cancellationToken).ConfigureAwait(false);
        return (ticket, options);
    }

    /// <summary>
    /// 换出登记的挑战并作废票据
    /// </summary>
    /// <param name="ticket">发起登记时给的票据</param>
    /// <param name="userId">确认登记的人（须是发起的人）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>挑战；票据不存在、过期、用过或不是这个人发起的为空</returns>
    public async Task<string?> ConsumeRegistrationAsync(string ticket, Guid userId, CancellationToken cancellationToken)
    {
        var stored = await ConsumeAsync(RegistrationPrefix, ticket, cancellationToken).ConfigureAwait(false);
        return stored?.Split('|', 2) is [var owner, var challenge] && string.Equals(owner, userId.ToString("N"), StringComparison.Ordinal) ? challenge : null;
    }

    /// <summary>
    /// 发起登录：生成请求选项，挑战记在票据上
    /// </summary>
    /// <remarks>
    /// 不列允许的凭据：由认证器列出本站的通行密钥让人选，登录之前不暴露任何账号信息
    /// </remarks>
    /// <param name="ceremonies">框架的仪式</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<(string Ticket, WebAuthnRequestOptions Options)> BeginAuthenticationAsync(WebAuthnCeremonies ceremonies, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ceremonies);
        var options = ceremonies.CreateAuthenticationOptions([]);
        var ticket = RefreshTokens.Generate();
        await cache.SetAsync(new CacheKey(AuthenticationPrefix + Hash(ticket)), options.Challenge, CacheEntryOptions.Expires(ceremonies.Options.Timeout), cancellationToken).ConfigureAwait(false);
        return (ticket, options);
    }

    /// <summary>
    /// 换出登录的挑战并作废票据
    /// </summary>
    /// <param name="ticket">发起登录时给的票据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>挑战；票据不存在、过期或用过为空</returns>
    public Task<string?> ConsumeAuthenticationAsync(string ticket, CancellationToken cancellationToken) => ConsumeAsync(AuthenticationPrefix, ticket, cancellationToken);

    /// <summary>
    /// 用户句柄：用户 ID 的 16 个字节（base64url）
    /// </summary>
    /// <param name="userId">用户</param>
    public static string UserHandleOf(Guid userId) => Base64Url.EncodeToString(userId.ToByteArray());

    /// <summary>
    /// 交给框架核对断言的存储凭据
    /// </summary>
    /// <param name="passkey">通行密钥</param>
    public static WebAuthnStoredCredential StoredOf(SysUserPasskey passkey)
    {
        ArgumentNullException.ThrowIfNull(passkey);
        return new WebAuthnStoredCredential(passkey.CredentialId, passkey.PublicKey, (uint)passkey.SignCount, UserHandleOf(passkey.UserId));
    }

    private static WebAuthnCredentialDescriptor DescriptorOf(SysUserPasskey passkey)
        => new("public-key", passkey.CredentialId, string.IsNullOrEmpty(passkey.Transports) ? null : passkey.Transports.Split(',', StringSplitOptions.RemoveEmptyEntries));

    private async Task<string?> ConsumeAsync(string prefix, string ticket, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return null;
        }

        var key = new CacheKey(prefix + Hash(ticket));
        var stored = await cache.GetAsync<string>(key, cancellationToken).ConfigureAwait(false);
        // 删除成功的那一次才算换出：并发提交同一张票据时只有一个拿得到挑战
        return stored.TryGetValue(out var value) && await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false) ? value : null;
    }

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
