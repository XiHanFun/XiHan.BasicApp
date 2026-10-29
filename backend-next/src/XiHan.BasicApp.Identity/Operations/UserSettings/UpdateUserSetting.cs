// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.UserSettings;

/// <summary>
/// 写本人的一条设置：<c>PUT /api/user-settings</c>（自助操作，只要求已认证）。按（用户, 场景, 设置键）有则覆盖、无则新建；值为 <c>null</c> 即清空。
/// 写入提交后经实时连接把新值推给这个用户的所有端（<c>UserSettingChanged</c>，带请求里的客户端标识，发起端据此忽略回显）。
/// </summary>
/// <param name="settings">用户设置仓储。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="pushes">实时推送（没装 Realtime 即没有）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class UpdateUserSetting(IRepository<SysUserSetting, Guid> settings, IIdGenerator<Guid> ids, IEnumerable<IRealtimePush> pushes) : IOperation<UpdateUserSettingRequest, UserSettingResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "user-settings.update";

    /// <inheritdoc />
    public async ValueTask<Result<UserSettingResponse>> HandleAsync(UpdateUserSettingRequest request, InvocationContext context)
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
        Result written;
        SysUserSetting setting;
        if (rows.Count == 0)
        {
            setting = new SysUserSetting { Id = ids.NewId(), UserId = userId, Scene = scene, Key = key, Value = request.Value };
            written = await settings.AddAsync(setting, context.CancellationToken).ConfigureAwait(false);
        }
        else
        {
            setting = rows[0];
            setting.Value = request.Value;
            written = await settings.UpdateAsync(setting, context.CancellationToken).ConfigureAwait(false);
        }

        if (written.IsFailure)
        {
            return written.Error!;
        }

        // 设置随人走（不分租户）：推给这个用户在所有侧的连接
        foreach (var push in pushes)
        {
            var changed = new UserSettingChangedPayload(scene, key, setting.Value, request.ClientId, request.Origin);
            await push.SendAsync(RealtimeAudience.Users([userId]), RealtimeMethods.UserSettingChanged, changed, IdentityJsonContext.Default.UserSettingChangedPayload, context.CancellationToken).ConfigureAwait(false);
        }

        return new UserSettingResponse(scene, key, setting.Value, setting.ModifiedAt ?? setting.CreatedAt);
    }
}
