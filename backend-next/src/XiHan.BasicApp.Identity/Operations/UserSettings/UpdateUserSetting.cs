// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
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
/// 其他端的实时回推随 Realtime 包接入（方案 5.11）。
/// </summary>
/// <param name="settings">用户设置仓储。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class UpdateUserSetting(IRepository<SysUserSetting, Guid> settings, IIdGenerator<Guid> ids) : IOperation<UpdateUserSettingRequest, UserSettingResponse>
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

        return written.IsFailure ? written.Error! : new UserSettingResponse(scene, key, setting.Value, setting.ModifiedAt ?? setting.CreatedAt);
    }
}
