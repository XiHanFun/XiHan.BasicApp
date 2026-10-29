// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.OpenPlatform.Domain.Services;
using XiHan.BasicApp.OpenPlatform.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Operations.OauthApps;

/// <summary>
/// 分页列出 OAuth 应用：<c>POST /api/oauth-apps:list</c>（平台建的与用户在「我的应用」里建的都在内，密钥不回显）。
/// </summary>
/// <param name="queries">查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OpenPlatformPermissions.OAuthAppsRead)]
public sealed class ListOAuthApps(OAuthAppQueries queries) : IOperation<PageRequest, PageResult<OAuthAppResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-apps.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<OAuthAppResponse>>> HandleAsync(PageRequest request, InvocationContext context) => await queries.ListAsync(request, context).ConfigureAwait(false);
}

/// <summary>
/// 取一个 OAuth 应用：<c>GET /api/oauth-apps/{id}</c>。
/// </summary>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OpenPlatformPermissions.OAuthAppsRead)]
public sealed class GetOAuthApp(OAuthAppService service) : IOperation<OAuthAppIdRequest, OAuthAppResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-apps.get";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthAppResponse>> HandleAsync(OAuthAppIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var app) ? OAuthAppResponse.From(app) : found.Error!;
    }
}

/// <summary>
/// 新建 OAuth 应用：<c>POST /api/oauth-apps</c>（ClientId 重复 <c>OPENPLATFORM_CONFLICT_002</c>；机密客户端的密钥只在这一次返回，4.x 管理端建不了公开客户端，这里可以）。
/// </summary>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OpenPlatformPermissions.OAuthAppsCreate)]
public sealed class CreateOAuthApp(OAuthAppService service) : IOperation<CreateOAuthAppRequest, OAuthAppSecretResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-apps.create";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthAppSecretResponse>> HandleAsync(CreateOAuthAppRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateAsync(request.ClientId, request.ClientType, request.ToDraft(), null, null, context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var pair) ? new OAuthAppSecretResponse(pair.App.Id, pair.App.ClientId, pair.App.ClientType, pair.Secret) : created.Error!;
    }
}

/// <summary>
/// 修改 OAuth 应用：<c>PUT /api/oauth-apps/{id}</c>（整体覆盖，含启停；ClientId、客户端类型与密钥不变）。
/// </summary>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OpenPlatformPermissions.OAuthAppsUpdate)]
public sealed class UpdateOAuthApp(OAuthAppService service) : IOperation<UpdateOAuthAppRequest, OAuthAppResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-apps.update";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthAppResponse>> HandleAsync(UpdateOAuthAppRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var app))
        {
            return found.Error!;
        }

        var updated = await service.UpdateAsync(app, request.ToDraft(), context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : OAuthAppResponse.From(app);
    }
}

/// <summary>
/// 重置 OAuth 应用的密钥：<c>POST /api/oauth-apps/{id}:reset-secret</c>（旧密钥立即失效；公开客户端 <c>OPENPLATFORM_CONFLICT_003</c>）。
/// </summary>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OpenPlatformPermissions.OAuthAppsUpdate)]
public sealed class ResetOAuthAppSecret(OAuthAppService service) : IOperation<OAuthAppIdRequest, OAuthAppSecretResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-apps.reset-secret";

    /// <inheritdoc />
    public async ValueTask<Result<OAuthAppSecretResponse>> HandleAsync(OAuthAppIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var app))
        {
            return found.Error!;
        }

        var reset = await service.ResetSecretAsync(app, context.CancellationToken).ConfigureAwait(false);
        return reset.TryGetValue(out var secret) ? new OAuthAppSecretResponse(app.Id, app.ClientId, app.ClientType, secret) : reset.Error!;
    }
}

/// <summary>
/// 批量删除 OAuth 应用：<c>DELETE /api/oauth-apps?ids=</c>（软删除，发出的令牌随应用失效；4.x 有授权码或令牌就不让删，记录又没有清理，应用永远删不掉）。
/// </summary>
/// <param name="service">应用服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OpenPlatformPermissions.OAuthAppsDelete)]
public sealed class DeleteOAuthApps(OAuthAppService service) : IOperation<OAuthAppIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "oauth-apps.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(OAuthAppIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.DeleteAsync(request.Ids, null, context.CancellationToken).ConfigureAwait(false);
    }
}
