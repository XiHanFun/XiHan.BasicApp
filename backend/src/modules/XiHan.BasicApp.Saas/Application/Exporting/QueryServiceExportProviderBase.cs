// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
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
/// <typeparam name="TRowDto">资源列表行 DTO（写出前由基类按发起人打码）</typeparam>
public abstract class QueryServiceExportProviderBase<TQueryDto, TRowDto> : IExportProvider
    where TQueryDto : BasicAppPRDto, new()
{
    /// <summary>
    /// 查询快照反序列化选项（Web 默认：camelCase + 大小写不敏感 + 数字可读字符串）
    /// </summary>
    /// <remarks>
    /// 快照是页面查询入参原样序列化的结果，与线上报文同形：枚举按成员名传（全局 JsonStringEnumConverter）。
    /// 这里不认成员名，带枚举筛选的快照就整体反序列化失败，导出任务会以「查询条件无法解析」失败。
    /// </remarks>
    protected static readonly JsonSerializerOptions QueryJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// 业务类型（= 导出按钮所属页面码，见 PageRegistry）
    /// </summary>
    public abstract string BusinessType { get; }

    /// <summary>
    /// 导出所需权限码（与页面导出按钮绑定的权限一致；提交时拦截，执行器进程内再校验一次）
    /// </summary>
    public abstract string RequiredPermission { get; }

    /// <summary>
    /// 流式读取导出行（按 context.Columns 顺序投影为字符串；遵循 context.Scope 单页/全量）
    /// </summary>
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
            // 进程内直调不经过 HTTP 响应过滤器，写出前按发起人打码，导出与在线列表同一口径
            await context.FieldSecurity.MaskAsync(page, cancellationToken);

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
    /// 反序列化查询快照为资源查询 DTO（未带快照即不加筛选；快照解析不了直接抛出，任务按失败收口）
    /// </summary>
    /// <remarks>
    /// 解析失败不能落回空查询：空查询等于不加任何筛选，导出范围会悄悄从「当前筛选结果」变成全量。
    /// </remarks>
    /// <exception cref="InvalidOperationException">快照不是该资源的查询条件</exception>
    protected virtual TQueryDto Deserialize(string? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
        {
            return new TQueryDto();
        }

        TQueryDto? query;
        try
        {
            query = JsonSerializer.Deserialize<TQueryDto>(snapshot, QueryJsonOptions);
        }
        catch (JsonException ex)
        {
            var position = string.IsNullOrEmpty(ex.Path) ? string.Empty : $"（{ex.Path}）";
            throw new InvalidOperationException($"查询条件无法解析{position}，导出已终止。", ex);
        }

        return query ?? throw new InvalidOperationException("查询条件无法解析（快照为 null），导出已终止。");
    }

    /// <summary>
    /// 调用对应 QueryService 的分页方法（子类实现）
    /// </summary>
    protected abstract Task<PageResultDtoBase<TRowDto>> QueryPageAsync(TQueryDto query, CancellationToken cancellationToken);
}
