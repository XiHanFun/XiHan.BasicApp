// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.BasicApp.Settings.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Dicts;

/// <summary>
/// 字典选项
/// </summary>
/// <remarks>
/// <c>GET /api/dicts:options?code=</c>（声明只读，ADR-0041）。业务表单的下拉用它，只要求已登录
/// 在可见的字典（平台的与本租户的）里按编码找；树形字典深度优先展平；停用项照常给出并标为停用；停用的字典没有选项；没有这个编码 <c>SETTINGS_NOTFOUND_002</c>
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。选项按侧缓存（<see cref="DictOptionSets"/>），本侧没有这个编码再看平台的
/// </remarks>
/// <param name="sets">字典选项缓存</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Policy = Policies.Authenticated)]
public sealed class DictOptions(DictOptionSets sets) : IOperation<DictOptionsRequest, DictOptionsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "dicts.options";

    /// <inheritdoc />
    public async ValueTask<Result<DictOptionsResponse>> HandleAsync(DictOptionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var code = request.Code.Trim();
        var owner = SettingsScope.OwnerOf(context);
        var options = (SettingsScope.IsPlatform(owner) ? null : await sets.OfAsync(owner, code, context.CancellationToken).ConfigureAwait(false))
            ?? await sets.OfAsync(TenantIds.Platform, code, context.CancellationToken).ConfigureAwait(false);
        if (options is null)
        {
            return Error.NotFound(SettingsErrors.DictNotFound, SettingsMessages.DictCodeNotFound, MessageArguments.Of(("code", code)));
        }

        return options;
    }
}
