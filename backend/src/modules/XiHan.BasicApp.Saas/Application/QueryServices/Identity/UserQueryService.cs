#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserQueryService
// Guid:7ff333fb-54a4-4dc5-ad98-b3a893fcaecd
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
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户")]
public sealed class UserQueryService(IUserRepository userRepository)
    : SaasApplicationService, IUserQueryService
{
    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 获取用户分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.User.Read)]
    public async Task<PageResultDtoBase<UserListItemDto>> GetUserPageAsync(UserPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildUserPageRequest(input);
        var users = await _userRepository.GetPagedAsync(request, cancellationToken);

        return users.Map(UserApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    /// <param name="id">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.User.Read)]
    public async Task<UserDetailDto?> GetUserDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : UserApplicationMapper.ToDetailDto(user);
    }

    /// <summary>
    /// 获取已启用用户选择项
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已启用用户选择项</returns>
    [PermissionAuthorize(SaasPermissionCodes.User.Read)]
    public async Task<IReadOnlyList<UserSelectItemDto>> GetEnabledUsersAsync(UserSelectQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildUserSelectRequest(input);
        var users = await _userRepository.GetPagedAsync(request, cancellationToken);

        return [.. users.Items.Select(UserApplicationMapper.ToSelectItemDto)];
    }

    /// <summary>
    /// 构建用户分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>用户分页请求</returns>
    private static BasicAppPRDto BuildUserPageRequest(UserPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        ApplyCommonUserFilters(
            request,
            input.Keyword,
            input.Gender,
            input.Status,
            input.IsSystemAccount,
            input.Language,
            input.Country);
        ApplyUserSorts(request);
        return request;
    }

    /// <summary>
    /// 构建用户选择请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>用户选择请求</returns>
    private static BasicAppPRDto BuildUserSelectRequest(UserSelectQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        request.Page.PageSize = Math.Clamp(input.Limit, 1, 500);
        ApplyCommonUserFilters(
            request,
            input.Keyword,
            input.Gender,
            EnableStatus.Enabled,
            input.IsSystemAccount,
            language: null,
            country: null);
        ApplyUserSorts(request);
        return request;
    }

    /// <summary>
    /// 应用用户通用筛选条件
    /// </summary>
    private static void ApplyCommonUserFilters(
        BasicAppPRDto request,
        string? keyword,
        UserGender? gender,
        EnableStatus? status,
        bool? isSystemAccount,
        string? language,
        string? country)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            request.Conditions.SetKeyword(
                keyword.Trim(),
                nameof(SysUser.UserName),
                nameof(SysUser.RealName),
                nameof(SysUser.NickName),
                nameof(SysUser.Country),
                nameof(SysUser.Remark));
        }

        if (gender.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUser.Gender), gender.Value);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUser.Status), status.Value);
        }

        if (isSystemAccount.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUser.IsSystemAccount), isSystemAccount.Value);
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            request.Conditions.AddFilter(nameof(SysUser.Language), language.Trim());
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            request.Conditions.AddFilter(nameof(SysUser.Country), country.Trim());
        }
    }

    /// <summary>
    /// 应用用户排序
    /// </summary>
    private static void ApplyUserSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort(nameof(SysUser.IsSystemAccount), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysUser.CreatedTime), SortDirection.Descending, 1);
        request.Conditions.AddSort(nameof(SysUser.UserName), SortDirection.Ascending, 2);
    }
}
