// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
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

    private readonly ICurrentUser _currentUser;

    private readonly IExportTaskRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExportTaskQueryService(IExportTaskRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<PageResultDtoBase<ExportTaskDto>> GetMineAsync(int pageIndex = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var normalizedPageIndex = Math.Max(1, pageIndex);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, total) = await _repository.GetMineAsync(userId, normalizedPageIndex, normalizedPageSize, cancellationToken);
        var dtos = items.Select(ExportTaskAppService.ToDto).ToList();
        return new PageResultDtoBase<ExportTaskDto>(dtos, normalizedPageIndex, normalizedPageSize, total);
    }

    /// <inheritdoc />
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
}
