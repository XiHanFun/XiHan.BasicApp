// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Configs;

/// <summary>
/// 参数详情：<c>GET /api/configs/{id}</c>。平台参数谁都能看，租户参数只有它自己；加密值不回显。
/// </summary>
/// <param name="configs">参数仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.ConfigsRead)]
public sealed class GetConfig(IRepository<SysConfig, Guid> configs) : IOperation<GetConfigRequest, ConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "configs.get";

    /// <inheritdoc />
    public async ValueTask<Result<ConfigResponse>> HandleAsync(GetConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        if (!(await configs.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var config) || !SettingsScope.IsVisible(config, owner))
        {
            return Error.NotFound(SettingsErrors.ConfigNotFound, SettingsMessages.ConfigNotFound, MessageArguments.Of(("id", request.Id)));
        }

        return ConfigResponse.From(config);
    }
}
