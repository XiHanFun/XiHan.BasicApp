// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.OpenPlatform.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Operations.OauthSigningKeys;

/// <summary>
/// 列出 id_token 的签名密钥
/// </summary>
/// <remarks>
/// <c>GET /api/oauth-signing-keys</c>（含停用的，新的在前；私钥不回显）
/// </remarks>
/// <param name="service">签名密钥服务</param>
/// <param name="clock">时间源</param>
[Operation(Id, ReadOnly = true, Expose = Expose.Http, Permission = OpenPlatformPermissions.OAuthSigningKeysRead)]
public sealed class ListOAuthSigningKeys(OAuthSigningKeyService service, TimeProvider clock) : IOperation<OAuthSigningKeysRequest, OAuthSigningKeysResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "oauth-signing-keys.list";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthSigningKeysResponse>> HandleAsync(OAuthSigningKeysRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return OAuthSigningKeysResponse.From(await service.ListAsync(context.CancellationToken).ConfigureAwait(false), clock.GetUtcNow());
    }
}

/// <summary>
/// 轮换 id_token 的签名密钥
/// </summary>
/// <remarks>
/// <c>POST /api/oauth-signing-keys:rotate</c>，返回轮换后的全部密钥。新密钥即刻公布，过一段时间接替签名，旧密钥留着公布；
/// 已有一把待接替的即 <c>OPENPLATFORM_CONFLICT_004</c>
/// </remarks>
/// <param name="service">签名密钥服务</param>
/// <param name="clock">时间源</param>
[Operation(Id, Expose = Expose.Http, Permission = OpenPlatformPermissions.OAuthSigningKeysRotate)]
public sealed class RotateOAuthSigningKey(OAuthSigningKeyService service, TimeProvider clock) : IOperation<OAuthSigningKeysRequest, OAuthSigningKeysResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "oauth-signing-keys.rotate";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthSigningKeysResponse>> HandleAsync(OAuthSigningKeysRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rotated = await service.RotateAsync(context.CancellationToken).ConfigureAwait(false);
        return rotated.IsFailure ? rotated.Error! : OAuthSigningKeysResponse.From(await service.ListAsync(context.CancellationToken).ConfigureAwait(false), clock.GetUtcNow());
    }
}

/// <summary>
/// 停用一把签名密钥
/// </summary>
/// <remarks>
/// <c>POST /api/oauth-signing-keys/{id}:retire</c>，返回停用后的全部密钥。JWKS 不再公布它，它签过的 id_token 随之验不过；
/// 正在签名的即 <c>OPENPLATFORM_CONFLICT_005</c>，不存在即 <c>OPENPLATFORM_NOTFOUND_003</c>
/// </remarks>
/// <param name="service">签名密钥服务</param>
/// <param name="clock">时间源</param>
[Operation(Id, Expose = Expose.Http, Permission = OpenPlatformPermissions.OAuthSigningKeysRetire)]
public sealed class RetireOAuthSigningKey(OAuthSigningKeyService service, TimeProvider clock) : IOperation<OAuthSigningKeyIdRequest, OAuthSigningKeysResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "oauth-signing-keys.retire";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthSigningKeysResponse>> HandleAsync(OAuthSigningKeyIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var retired = await service.RetireAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return retired.IsFailure ? retired.Error! : OAuthSigningKeysResponse.From(await service.ListAsync(context.CancellationToken).ConfigureAwait(false), clock.GetUtcNow());
    }
}
