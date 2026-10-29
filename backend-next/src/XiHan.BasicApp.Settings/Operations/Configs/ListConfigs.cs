// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Settings.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Configs;

/// <summary>
/// 参数列表。
/// </summary>
/// <remarks>
/// <c>POST /api/configs:list</c>。读共享：平台主体看平台参数，租户主体看平台参数与本租户参数；加密值不回显。
/// </remarks>
/// <param name="queries">参数查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.ConfigsRead)]
public sealed class ListConfigs(ConfigQueries queries) : IOperation<PageRequest, PageResult<ConfigResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "configs.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<ConfigResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
