// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.UserSettings;

/// <summary>
/// 读本人的一条设置。
/// </summary>
/// <remarks>
/// <c>GET /api/user-settings?scene=&amp;key=</c>（自助操作，只要求已认证）。还没存过即返回空值，前端用本地缺省。
/// 模仿中读的是被模仿用户的设置（令牌主体即被模仿者）。
/// </remarks>
/// <param name="settings">用户设置仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetUserSetting(IRepository<SysUserSetting, Guid> settings) : IOperation<GetUserSettingRequest, UserSettingResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "user-settings.get";

    /// <inheritdoc />
    public async ValueTask<Result<UserSettingResponse>> HandleAsync(GetUserSettingRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var principal = context.GetSecurity().Principal;
        if (!Guid.TryParse(principal.Id, out var userId))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", principal.Id)));
        }

        var scene = request.Scene;
        var key = request.Key;
        var rows = await settings.ListAsync(EntityQuery.All<SysUserSetting>().Where(new Specification<SysUserSetting>(s => s.UserId == userId && s.Scene == scene && s.Key == key)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        return rows.Count == 0
            ? new UserSettingResponse(scene, key, null, null)
            : new UserSettingResponse(scene, key, rows[0].Value, rows[0].ModifiedAt ?? rows[0].CreatedAt);
    }
}
