#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PositionQueryService
// Guid:e57643b0-394a-44c5-582d-364758607182
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 岗位查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "岗位")]
public sealed class PositionQueryService
    : SaasApplicationService, IPositionQueryService
{
    /// <summary>
    /// 岗位仓储
    /// </summary>
    private readonly IPositionRepository _positionRepository;

    /// <summary>
    /// 字段级安全（排序门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PositionQueryService(IPositionRepository positionRepository, IFieldSecurityService fieldSecurityService)
    {
        _positionRepository = positionRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取岗位分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>岗位分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Position.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<PositionListItemDto>> GetPositionPageAsync(PositionPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPositionPageRequest(input);

        // 过滤：前端区间(Between)/多选(In)等条件经 conditions.filters 下发，FLS 门控剔除不可读/已脱敏字段后由框架统一应用
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysPosition", cancellationToken);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysPosition", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyPositionSorts(request);
        }

        var positionPage = await _positionRepository.GetPagedAsync(request, cancellationToken);
        if (positionPage.Items.Count == 0)
        {
            return new PageResultDtoBase<PositionListItemDto>([], positionPage.Page)
            {
                ExtendDatas = positionPage.ExtendDatas
            };
        }

        var items = positionPage.Items
            .Select(PositionApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<PositionListItemDto>(items, positionPage.Page)
        {
            ExtendDatas = positionPage.ExtendDatas
        };
    }

    /// <summary>
    /// 获取岗位详情
    /// </summary>
    /// <param name="id">岗位主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>岗位详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Position.Read)]
    public async Task<PositionDetailDto?> GetPositionDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "岗位主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var position = await _positionRepository.GetByIdAsync(id, cancellationToken);
        return position is null ? null : PositionApplicationMapper.ToDetailDto(position);
    }

    /// <summary>
    /// 构建岗位分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>岗位分页请求</returns>
    private static BasicAppPRDto BuildPositionPageRequest(PositionPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysPosition>(
                input.Keyword.Trim(),
                position => position.PositionName,
                position => position.PositionCode,
                position => position.Remark);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysPosition position) => position.Status, input.Status.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetPositionPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        // 前端区间/多选过滤原样带入（FLS 门控在调用方 GetPositionPageAsync 处理）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }
        return request;
    }

    /// <summary>
    /// 应用岗位默认排序
    /// </summary>
    private static void ApplyPositionSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysPosition position) => position.Sort, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysPosition position) => position.PositionCode, SortDirection.Ascending, 1);
    }
}
