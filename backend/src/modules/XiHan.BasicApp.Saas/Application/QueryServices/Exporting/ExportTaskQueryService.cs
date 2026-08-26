// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 导出任务查询应用服务（读侧：当前用户的导出任务列表 / 详情，供导出中心展示与状态轮询）。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "导出任务")]
public sealed class ExportTaskQueryService
    : SaasApplicationService, IExportTaskQueryService
{
    /// <summary>
    /// 单页条数上限
    /// </summary>
    private const int MaxPageSize = 100;

    /// <summary>
    /// 允许参与排序 / 过滤的字段白名单（大小写不敏感，覆盖前端 camelCase 列键与实体 PascalCase 属性名）。
    /// 不在名单内的排序项与过滤项在下发仓储前被剔除。
    /// 与前端 export-center 页的列 schema 一一对应，加可搜索列时两边一起改。
    /// </summary>
    private static readonly IReadOnlySet<string> AllowedQueryFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(SysExportTask.BusinessType),
            nameof(SysExportTask.Status),
            nameof(SysExportTask.Scope),
            nameof(SysExportTask.Format),
            nameof(SysExportTask.CreatedTime),
            nameof(SysExportTask.FinishedTime),
            nameof(SysExportTask.TaskName)
        };

    private readonly ICurrentUser _currentUser;

    private readonly IFieldSecurityService _fieldSecurity;

    private readonly IExportTaskRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExportTaskQueryService(IExportTaskRepository repository, ICurrentUser currentUser, IFieldSecurityService fieldSecurityService)
    {
        _repository = repository;
        _currentUser = currentUser;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取当前用户的导出任务分页（支持关键字 / 多选 / 时间区间；无排序时按创建时间倒序）
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导出任务分页列表</returns>
    [HttpPost]
    public async Task<PageResultDtoBase<ExportTaskDto>> GetMineAsync(ExportTaskPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");

        var request = BuildPageRequest(input);

        // 白名单收窄：只放行导出中心列表在用的列，其余排序 / 过滤项剔除
        KeepAllowedFieldsOnly(request.Conditions);

        // FLS 门控：再剔除当前用户不可读或已脱敏的字段
        await _fieldSecurity.GuardSortsAsync(request.Conditions, nameof(SysExportTask), cancellationToken);
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, nameof(SysExportTask), cancellationToken);

        // 无有效排序时回退默认排序（GetPagedAsync 不带默认排序，缺排序会让翻页在库侧无序返回，出现重复行与漏行）
        if (request.Conditions.Sorts.Count == 0)
        {
            request.Conditions.AddSort((SysExportTask task) => task.CreatedTime, SortDirection.Descending, 0);
        }

        // 自鉴权：数据边界锁定为本人创建的任务
        var paged = await _repository.GetPagedAsync(request, task => task.CreatedId == userId, cancellationToken);
        return paged.Map(ExportTaskAppService.ToDto);
    }

    /// <summary>
    /// 获取当前用户的导出任务详情（自鉴权；不存在返回 null）
    /// </summary>
    public async Task<ExportTaskDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "任务主键必须大于 0。");
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var entity = await _repository.GetByIdForUserAsync(id, userId, cancellationToken);
        return entity is null ? null : ExportTaskAppService.ToDto(entity);
    }

    /// <summary>
    /// 构建导出任务分页请求（页参数归一 + 关键字 + 前端排序 / 过滤带入）
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>导出任务分页请求</returns>
    private static BasicAppPRDto BuildPageRequest(ExportTaskPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = new PageRequestMetadata(
                Math.Max(1, input.Page.PageIndex),
                Math.Clamp(input.Page.PageSize, 1, MaxPageSize)),
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysExportTask>(
                input.Keyword.Trim(),
                task => task.TaskName,
                task => task.BusinessType,
                task => task.FileName);
        }

        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        return request;
    }

    /// <summary>
    /// 按白名单就地剔除排序 / 过滤中不允许的字段
    /// </summary>
    /// <param name="conditions">查询条件</param>
    private static void KeepAllowedFieldsOnly(QueryConditions conditions)
    {
        _ = conditions.Sorts.RemoveAll(sort => !AllowedQueryFields.Contains(sort.Field));
        _ = conditions.Filters.RemoveAll(filter => !AllowedQueryFields.Contains(filter.Field));
    }
}
