// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Dicts;

/// <summary>
/// 字典选项。
/// </summary>
/// <remarks>
/// <c>GET /api/dicts:options?code=</c>（声明只读，ADR-0041）。业务表单的下拉用它，只要求已登录。
/// 在可见的字典（平台的与本租户的）里按编码找；树形字典深度优先展平；停用项照常给出并标为停用；停用的字典没有选项；没有这个编码 <c>SETTINGS_NOTFOUND_002</c>。
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。
/// </remarks>
/// <param name="service">字典域服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Policy = Policies.Authenticated)]
public sealed class DictOptions(DictService service) : IOperation<DictOptionsRequest, DictOptionsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dicts.options";

    /// <inheritdoc />
    public async ValueTask<Result<DictOptionsResponse>> HandleAsync(DictOptionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var code = request.Code.Trim();
        if (!(await service.FindVisibleByCodeAsync(code, SettingsScope.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var dict))
        {
            return Error.NotFound(SettingsErrors.DictNotFound, SettingsMessages.DictCodeNotFound, MessageArguments.Of(("code", code)));
        }

        if (dict.Status != SettingStatus.Active)
        {
            return new DictOptionsResponse([]);
        }

        var all = await service.ItemsOfAsync(dict.Id, context.CancellationToken).ConfigureAwait(false);
        var codes = all.ToDictionary(static i => i.Id, static i => i.Code);
        var options = DictService.Flatten(all)
            .Select(entry => new DictOption(
                entry.Item.Code,
                entry.Item.Name,
                entry.Item.ParentId is { } parentId && codes.TryGetValue(parentId, out var parentCode) ? parentCode : null,
                entry.Item.IsDefault,
                entry.Item.Status != SettingStatus.Active))
            .ToList();
        return new DictOptionsResponse(options);
    }
}
