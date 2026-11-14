// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.UserSettings;

/// <summary>
/// 读本人的一条设置
/// </summary>
/// <remarks>
/// <c>GET /api/user-settings?scene=&amp;key=</c>（自助操作，只要求已认证）。还没存过即返回空值，前端用本地缺省
/// 模仿中读的是被模仿用户的设置（令牌主体即被模仿者）。经缓存读（<see cref="UserSettingEntries"/>）
/// </remarks>
/// <param name="entries">用户设置缓存</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetUserSetting(UserSettingEntries entries) : IOperation<GetUserSettingRequest, UserSettingResponse>
{
    /// <summary>
    /// 操作 ID
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

        return await entries.OfAsync(userId, request.Scene, request.Key, context.CancellationToken).ConfigureAwait(false);
    }
}
