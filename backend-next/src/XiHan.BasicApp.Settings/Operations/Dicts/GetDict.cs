// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Dicts;

/// <summary>
/// 字典详情：<c>GET /api/dicts/{id}</c>。平台字典谁都能看，租户字典只有它自己。只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。
/// </summary>
/// <param name="service">字典域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = SettingsPermissions.DictsRead)]
public sealed class GetDict(DictService service) : IOperation<GetDictRequest, DictResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dicts.get";

    /// <inheritdoc />
    public async ValueTask<Result<DictResponse>> HandleAsync(GetDictRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindVisibleAsync(request.Id, SettingsScope.OwnerOf(context), context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var dict)
            ? DictResponse.From(dict)
            : Error.NotFound(SettingsErrors.DictNotFound, SettingsMessages.DictNotFound, MessageArguments.Of(("id", request.Id)));
    }
}
