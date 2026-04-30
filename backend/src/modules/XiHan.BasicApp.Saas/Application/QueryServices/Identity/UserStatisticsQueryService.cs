#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserStatisticsQueryService
// Guid:1f102d2a-0e4e-4c7f-b2fe-97e083069f0a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
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
/// 用户统计查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户统计")]
public sealed class UserStatisticsQueryService(
    IUserStatisticsRepository userStatisticsRepository,
    IUserRepository userRepository)
    : SaasApplicationService, IUserStatisticsQueryService
{
    /// <summary>
    /// 用户统计仓储
    /// </summary>
    private readonly IUserStatisticsRepository _userStatisticsRepository = userStatisticsRepository;

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 获取用户统计分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户统计分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserStatistics.Read)]
    public async Task<PageResultDtoBase<UserStatisticsListItemDto>> GetUserStatisticsPageAsync(UserStatisticsPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildUserStatisticsPageRequest(input);
        var statisticsPage = await _userStatisticsRepository.GetPagedAsync(request, cancellationToken);
        if (statisticsPage.Items.Count == 0)
        {
            return new PageResultDtoBase<UserStatisticsListItemDto>([], statisticsPage.Page);
        }

        var userMap = await BuildUserMapAsync(statisticsPage.Items.Select(statistics => statistics.UserId), cancellationToken);
        var items = statisticsPage.Items
            .Select(statistics => UserStatisticsApplicationMapper.ToListItemDto(
                statistics,
                userMap.GetValueOrDefault(statistics.UserId)))
            .ToList();

        return new PageResultDtoBase<UserStatisticsListItemDto>(items, statisticsPage.Page);
    }

    /// <summary>
    /// 获取用户统计详情
    /// </summary>
    /// <param name="id">统计主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户统计详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserStatistics.Read)]
    public async Task<UserStatisticsDetailDto?> GetUserStatisticsDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "统计主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var statistics = await _userStatisticsRepository.GetByIdAsync(id, cancellationToken);
        if (statistics is null)
        {
            return null;
        }

        var user = statistics.UserId > 0
            ? await _userRepository.GetByIdAsync(statistics.UserId, cancellationToken)
            : null;

        return UserStatisticsApplicationMapper.ToDetailDto(statistics, user);
    }

    /// <summary>
    /// 构建用户统计分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>用户统计分页请求</returns>
    private static BasicAppPRDto BuildUserStatisticsPageRequest(UserStatisticsPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (input.UserId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserStatistics.UserId), input.UserId.Value);
        }

        if (input.Period.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserStatistics.Period), input.Period.Value);
        }

        if (input.StatisticsDateStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserStatistics.StatisticsDate), input.StatisticsDateStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.StatisticsDateEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserStatistics.StatisticsDate), input.StatisticsDateEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort(nameof(SysUserStatistics.StatisticsDate), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysUserStatistics.Period), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysUserStatistics.UserId), SortDirection.Ascending, 2);
        return request;
    }

    /// <summary>
    /// 构建用户映射
    /// </summary>
    /// <param name="userIds">用户主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户映射</returns>
    private async Task<IReadOnlyDictionary<long, SysUser>> BuildUserMapAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(userId => userId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysUser>();
        }

        var users = await _userRepository.GetByIdsAsync(ids, cancellationToken);
        return users.ToDictionary(user => user.BasicId);
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(UserStatisticsPageQueryDto input)
    {
        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键不能小于 0。");
        if (input.Period.HasValue)
        {
            ValidateEnum(input.Period.Value, nameof(input.Period));
        }

        if (input.StatisticsDateStart.HasValue &&
            input.StatisticsDateEnd.HasValue &&
            input.StatisticsDateStart.Value > input.StatisticsDateEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.StatisticsDateStart), "统计开始日期不能晚于结束日期。");
        }
    }

    /// <summary>
    /// 校验可空主键
    /// </summary>
    private static void ValidateOptionalId(long? id, string paramName, string message)
    {
        if (id is < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }
}
