// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Operations.Profile;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.MyApiCredentials;

/// <summary>
/// 本人的开放接口凭证：<c>GET /api/my-api-credentials</c>（密钥不回显）。已登录即可；模仿会话看到的是被模仿人的。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="service">凭证服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListMyApiCredentials(IRepository<SysUser, Guid> users, UserAccountService accounts, ApiCredentialService service) : IOperation<MyApiCredentialsRequest, MyApiCredentialsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-api-credentials.list";

    /// <inheritdoc />
    public async ValueTask<Result<MyApiCredentialsResponse>> HandleAsync(MyApiCredentialsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var rows = await service.ListAsync(found.User.Id, context.CancellationToken).ConfigureAwait(false);
        return new MyApiCredentialsResponse([.. rows.Select(MyApiCredentialResponse.From)]);
    }
}

/// <summary>
/// 新建开放接口凭证：<c>POST /api/my-api-credentials</c>。密钥只在这一次返回；每人最多 5 个（<c>IDENTITY_CONFLICT_007</c>）。模仿会话不能做。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="service">凭证服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class CreateMyApiCredential(IRepository<SysUser, Guid> users, UserAccountService accounts, ApiCredentialService service) : IOperation<CreateMyApiCredentialRequest, ApiCredentialSecretResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-api-credentials.create";

    /// <inheritdoc />
    public async ValueTask<Result<ApiCredentialSecretResponse>> HandleAsync(CreateMyApiCredentialRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var name = string.IsNullOrWhiteSpace(request.Name) ? context.Localize(IdentityMessages.ApiCredentialDefaultName) : request.Name.Trim();
        var created = await service.CreateAsync(found.User.Id, name, context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var pair) ? new ApiCredentialSecretResponse(pair.Credential.Id, pair.Credential.AppKey, pair.Secret) : created.Error!;
    }
}

/// <summary>
/// 启用或停用本人的一个凭证：<c>PUT /api/my-api-credentials/{id}</c>。模仿会话不能做。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="service">凭证服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class UpdateMyApiCredential(IRepository<SysUser, Guid> users, UserAccountService accounts, ApiCredentialService service) : IOperation<UpdateMyApiCredentialRequest, MyApiCredentialResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-api-credentials.update";

    /// <inheritdoc />
    public async ValueTask<Result<MyApiCredentialResponse>> HandleAsync(UpdateMyApiCredentialRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var owned = await service.FindOwnedAsync(found.User.Id, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!owned.TryGetValue(out var credential))
        {
            return owned.Error!;
        }

        var updated = await service.SetEnabledAsync(credential, request.IsEnabled, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : MyApiCredentialResponse.From(credential);
    }
}

/// <summary>
/// 滚动本人一个凭证的密钥：<c>POST /api/my-api-credentials/{id}:rotate-secret</c>。旧密钥立即失效，新密钥只在这一次返回。模仿会话不能做。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="service">凭证服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class RotateMyApiCredentialSecret(IRepository<SysUser, Guid> users, UserAccountService accounts, ApiCredentialService service) : IOperation<MyApiCredentialIdRequest, ApiCredentialSecretResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-api-credentials.rotate-secret";

    /// <inheritdoc />
    public async ValueTask<Result<ApiCredentialSecretResponse>> HandleAsync(MyApiCredentialIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var owned = await service.FindOwnedAsync(found.User.Id, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!owned.TryGetValue(out var credential))
        {
            return owned.Error!;
        }

        var rotated = await service.RotateAsync(credential, context.CancellationToken).ConfigureAwait(false);
        return rotated.TryGetValue(out var secret) ? new ApiCredentialSecretResponse(credential.Id, credential.AppKey, secret) : rotated.Error!;
    }
}

/// <summary>
/// 删除本人的凭证：<c>DELETE /api/my-api-credentials?ids=</c>（AppKey 立即不能用）。模仿会话不能做。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="service">凭证服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class DeleteMyApiCredentials(IRepository<SysUser, Guid> users, UserAccountService accounts, ApiCredentialService service) : IOperation<MyApiCredentialIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-api-credentials.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MyApiCredentialIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        return await service.DeleteAsync(found.User.Id, request.Ids, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 本人的调用安全设置：<c>GET /api/my-api-credentials:settings</c>（签名算法与 IP 白名单，作用于本人全部凭证）。已登录即可。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetMyApiSettings(IRepository<SysUser, Guid> users, UserAccountService accounts) : IOperation<MyApiCredentialsRequest, MyApiSettingsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-api-credentials.settings";

    /// <inheritdoc />
    public async ValueTask<Result<MyApiSettingsResponse>> HandleAsync(MyApiCredentialsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        return subject.TryGetValue(out var found)
            ? new MyApiSettingsResponse(ApiCredentialService.AlgorithmOf(found.Security), ApiCredentialService.AllowListOf(found.Security), ApiSignatureAlgorithms.All)
            : subject.Error!;
    }
}

/// <summary>
/// 写本人的调用安全设置：<c>POST /api/my-api-credentials:update-settings</c>（整体覆盖；算法不认识 <c>IDENTITY_VALIDATION_012</c>，白名单有一条不是 IP 或 CIDR <c>IDENTITY_VALIDATION_013</c>）。
/// 立即作用于本人全部凭证。模仿会话不能做。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="securities">账号安全仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class UpdateMyApiSettings(IRepository<SysUser, Guid> users, UserAccountService accounts, IRepository<SysUserSecurity, Guid> securities) : IOperation<UpdateMyApiSettingsRequest, MyApiSettingsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "my-api-credentials.update-settings";

    /// <inheritdoc />
    public async ValueTask<Result<MyApiSettingsResponse>> HandleAsync(UpdateMyApiSettingsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var applied = ApiCredentialService.ApplySettings(found.Security, request.SignatureAlgorithm, request.IpAllowList);
        if (applied.IsFailure)
        {
            return applied.Error!;
        }

        var updated = await securities.UpdateAsync(found.Security, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure
            ? updated.Error!
            : new MyApiSettingsResponse(ApiCredentialService.AlgorithmOf(found.Security), ApiCredentialService.AllowListOf(found.Security), ApiSignatureAlgorithms.All);
    }
}
