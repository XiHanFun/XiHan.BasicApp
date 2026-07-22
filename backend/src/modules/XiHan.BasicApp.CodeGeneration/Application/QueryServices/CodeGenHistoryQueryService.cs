// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Application.Mappers;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.CodeGeneration.Application.QueryServices;

/// <summary>
/// 代码生成历史查询应用服务（历史为系统写入，只读对外）
/// </summary>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "生成历史")]
public sealed class CodeGenHistoryQueryService : CodeGenerationApplicationService, ICodeGenHistoryQueryService
{
    /// <summary>
    /// 代码生成历史仓储
    /// </summary>
    private readonly ICodeGenHistoryRepository _historyRepository;

    /// <summary>
    /// 字段级安全（排序门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenHistoryQueryService(ICodeGenHistoryRepository historyRepository, IFieldSecurityService fieldSecurityService)
    {
        _historyRepository = historyRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取代码生成历史分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>代码生成历史分页列表</returns>
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<CodeGenHistoryListItemDto>> GetPageAsync(CodeGenHistoryPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysCodeGenHistory", cancellationToken);
        // 过滤：前端下发的区间/多选过滤经 FLS 门控剔除不可读/已脱敏字段
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysCodeGenHistory", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyHistorySorts(request);
        }

        var historyPage = await _historyRepository.GetPagedAsync(request, cancellationToken);
        if (historyPage.Items.Count == 0)
        {
            return new PageResultDtoBase<CodeGenHistoryListItemDto>([], historyPage.Page)
            {
                ExtendDatas = historyPage.ExtendDatas
            };
        }

        var items = historyPage.Items
            .Select(CodeGenHistoryApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<CodeGenHistoryListItemDto>(items, historyPage.Page)
        {
            ExtendDatas = historyPage.ExtendDatas
        };
    }

    /// <summary>
    /// 获取代码生成历史详情
    /// </summary>
    /// <param name="id">代码生成历史主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>代码生成历史详情</returns>
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<CodeGenHistoryDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "代码生成历史主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var history = await _historyRepository.GetByIdAsync(id, cancellationToken);
        return history is null ? null : CodeGenHistoryApplicationMapper.ToDetailDto(history);
    }

    /// <summary>
    /// 按表配置获取代码生成历史（按生成时间倒序）
    /// </summary>
    /// <param name="tableId">所属表ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>代码生成历史列表</returns>
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<IReadOnlyList<CodeGenHistoryListItemDto>> GetByTableAsync(long tableId, CancellationToken cancellationToken = default)
    {
        if (tableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tableId), "所属表ID必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var histories = await _historyRepository.GetByTableIdAsync(tableId, cancellationToken);
        return histories
            .Select(CodeGenHistoryApplicationMapper.ToListItemDto)
            .ToList();
    }

    /// <summary>
    /// 构建代码生成历史分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>代码生成历史分页请求</returns>
    private static BasicAppPRDto BuildPageRequest(CodeGenHistoryPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (input.TableId.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenHistory history) => history.TableId, input.TableId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.TableName))
        {
            request.Conditions.AddFilter((SysCodeGenHistory history) => history.TableName, input.TableName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.BatchNumber))
        {
            request.Conditions.AddFilter((SysCodeGenHistory history) => history.BatchNumber, input.BatchNumber.Trim());
        }

        if (input.GenStatus.HasValue)
        {
            request.Conditions.AddFilter((SysCodeGenHistory history) => history.GenStatus, input.GenStatus.Value);
        }

        if (input.StartTime.HasValue)
        {
            request.Conditions.AddFilter(
                (SysCodeGenHistory history) => history.GenTime,
                input.StartTime.Value,
                QueryOperator.GreaterThanOrEqual);
        }

        if (input.EndTime.HasValue)
        {
            request.Conditions.AddFilter(
                (SysCodeGenHistory history) => history.GenTime,
                input.EndTime.Value,
                QueryOperator.LessThanOrEqual);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        // 前端下发的过滤（区间 Between/枚举多选 In）原样带入（FLS 门控在调用方 GetPageAsync 处理）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }
        return request;
    }

    /// <summary>
    /// 应用历史默认排序
    /// </summary>
    private static void ApplyHistorySorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysCodeGenHistory history) => history.GenTime, SortDirection.Descending, 0);
    }
}
