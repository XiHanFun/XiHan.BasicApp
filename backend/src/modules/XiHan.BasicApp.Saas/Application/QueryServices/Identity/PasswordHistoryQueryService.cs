#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PasswordHistoryQueryService
// Guid:ef5cd005-6839-40be-8955-e1a9fbab2cf8
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
/// 密码历史查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "密码历史")]
public sealed class PasswordHistoryQueryService(
    IPasswordHistoryRepository passwordHistoryRepository,
    IUserRepository userRepository)
    : SaasApplicationService, IPasswordHistoryQueryService
{
    /// <summary>
    /// 密码历史仓储
    /// </summary>
    private readonly IPasswordHistoryRepository _passwordHistoryRepository = passwordHistoryRepository;

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 获取密码历史分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>密码历史分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.PasswordHistory.Read)]
    public async Task<PageResultDtoBase<PasswordHistoryListItemDto>> GetPasswordHistoryPageAsync(PasswordHistoryPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildPasswordHistoryPageRequest(input);
        var historyPage = await _passwordHistoryRepository.GetPagedAsync(request, cancellationToken);
        if (historyPage.Items.Count == 0)
        {
            return new PageResultDtoBase<PasswordHistoryListItemDto>([], historyPage.Page);
        }

        var userMap = await BuildUserMapAsync(historyPage.Items.Select(history => history.UserId), cancellationToken);
        var items = historyPage.Items
            .Select(history => PasswordHistoryApplicationMapper.ToListItemDto(
                history,
                userMap.GetValueOrDefault(history.UserId)))
            .ToList();

        return new PageResultDtoBase<PasswordHistoryListItemDto>(items, historyPage.Page);
    }

    /// <summary>
    /// 获取密码历史详情
    /// </summary>
    /// <param name="id">密码历史主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>密码历史详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.PasswordHistory.Read)]
    public async Task<PasswordHistoryDetailDto?> GetPasswordHistoryDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "密码历史主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var history = await _passwordHistoryRepository.GetByIdAsync(id, cancellationToken);
        if (history is null)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(history.UserId, cancellationToken);
        return PasswordHistoryApplicationMapper.ToDetailDto(history, user);
    }

    /// <summary>
    /// 构建密码历史分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>密码历史分页请求</returns>
    private static BasicAppPRDto BuildPasswordHistoryPageRequest(PasswordHistoryPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (input.UserId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPasswordHistory.UserId), input.UserId.Value);
        }

        if (input.ChangedTimeStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPasswordHistory.ChangedTime), input.ChangedTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ChangedTimeEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPasswordHistory.ChangedTime), input.ChangedTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort(nameof(SysPasswordHistory.ChangedTime), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysPasswordHistory.CreatedTime), SortDirection.Descending, 1);
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
    private static void ValidatePageInput(PasswordHistoryPageQueryDto input)
    {
        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        if (input.ChangedTimeStart.HasValue &&
            input.ChangedTimeEnd.HasValue &&
            input.ChangedTimeStart.Value > input.ChangedTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.ChangedTimeStart), "修改开始时间不能晚于结束时间。");
        }
    }

    /// <summary>
    /// 校验可空主键
    /// </summary>
    private static void ValidateOptionalId(long? id, string paramName, string message)
    {
        if (id is <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }
}
