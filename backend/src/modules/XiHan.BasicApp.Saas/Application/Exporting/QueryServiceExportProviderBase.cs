#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:QueryServiceExportProviderBase
// Guid:f1e8b6d4-9a2c-4d7f-86b0-6273849506b7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Runtime.CompilerServices;
using System.Text.Json;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 基于既有 QueryService 的导出 Provider 基类。
/// 子类只需声明业务类型/权限码 + 反序列化查询 DTO + 调用对应 QueryService 分页方法；
/// 基类负责翻页循环、首页回填总数、列投影、范围（单页/全量）与安全上限。
/// </summary>
/// <typeparam name="TQueryDto">资源自身分页查询 DTO（含 Page 分页元数据）</typeparam>
/// <typeparam name="TRowDto">资源列表行 DTO（已脱敏/已映射）</typeparam>
public abstract class QueryServiceExportProviderBase<TQueryDto, TRowDto> : IExportProvider
    where TQueryDto : BasicAppPRDto, new()
{
    /// <summary>
    /// 查询快照反序列化选项（Web 默认：camelCase + 大小写不敏感）
    /// </summary>
    protected static readonly JsonSerializerOptions QueryJsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public abstract string BusinessType { get; }

    /// <inheritdoc />
    public abstract string RequiredPermission { get; }

    /// <inheritdoc />
    public async IAsyncEnumerable<IReadOnlyList<string>> ReadRowsAsync(
        ExportContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var query = Deserialize(context.QuerySnapshot);
        var isCurrentPage = context.Scope == ExportScope.CurrentPage;
        var pageSize = isCurrentPage
            ? Math.Clamp(query.Page.PageSize, 1, ExportDefaults.CurrentPageMaxSize)
            : ExportDefaults.BatchSize;
        // 请求页大小不得超过框架分页上限（MaxPageSize），否则会被查询侧钳小，
        // 导致每页实际返回 < 请求 pageSize，被下方"拿到不足一页即最后一页"误判而提前结束（全量导出只导出首页）。
        pageSize = Math.Min(pageSize, PageRequestMetadata.MaxPageSize);
        var pageIndex = isCurrentPage ? Math.Max(1, query.Page.PageIndex) : 1;

        var emitted = 0;
        var first = true;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            query.Page.PageIndex = pageIndex;
            query.Page.PageSize = pageSize;
            var page = await QueryPageAsync(query, cancellationToken);

            if (first)
            {
                context.Total = isCurrentPage ? page.Items.Count : page.Page.TotalCount;
                first = false;
            }

            if (page.Items.Count == 0)
            {
                yield break;
            }

            foreach (var item in page.Items)
            {
                if (item is null)
                {
                    continue;
                }

                yield return DtoRowProjector.Project(item, context.Columns);
                emitted++;
                if (emitted >= ExportDefaults.MaxRows)
                {
                    yield break;
                }
            }

            if (isCurrentPage || page.Items.Count < pageSize)
            {
                yield break;
            }

            pageIndex++;
        }
    }

    /// <summary>
    /// 反序列化查询快照为资源查询 DTO（缺省返回空查询）
    /// </summary>
    protected virtual TQueryDto Deserialize(string? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
        {
            return new TQueryDto();
        }

        try
        {
            return JsonSerializer.Deserialize<TQueryDto>(snapshot, QueryJsonOptions) ?? new TQueryDto();
        }
        catch (JsonException)
        {
            return new TQueryDto();
        }
    }

    /// <summary>
    /// 调用对应 QueryService 的分页方法（子类实现）
    /// </summary>
    protected abstract Task<PageResultDtoBase<TRowDto>> QueryPageAsync(TQueryDto query, CancellationToken cancellationToken);
}
