#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSecurityQueryService
// Guid:a0dd13fa-673f-4f91-9880-c1a5373ac0f8
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
/// 用户安全查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户安全")]
public sealed class UserSecurityQueryService(
    IUserSecurityRepository userSecurityRepository,
    IUserRepository userRepository)
    : SaasApplicationService, IUserSecurityQueryService
{
    /// <summary>
    /// 用户安全仓储
    /// </summary>
    private readonly IUserSecurityRepository _userSecurityRepository = userSecurityRepository;

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 获取用户安全分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.Read)]
    public async Task<PageResultDtoBase<UserSecurityListItemDto>> GetUserSecurityPageAsync(UserSecurityPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildUserSecurityPageRequest(input);
        var securityPage = await _userSecurityRepository.GetPagedAsync(request, cancellationToken);
        if (securityPage.Items.Count == 0)
        {
            return new PageResultDtoBase<UserSecurityListItemDto>([], securityPage.Page);
        }

        var now = DateTimeOffset.UtcNow;
        var userMap = await BuildUserMapAsync(securityPage.Items.Select(security => security.UserId), cancellationToken);
        var items = securityPage.Items
            .Select(security => UserSecurityApplicationMapper.ToListItemDto(
                security,
                userMap.GetValueOrDefault(security.UserId),
                now))
            .ToList();

        return new PageResultDtoBase<UserSecurityListItemDto>(items, securityPage.Page);
    }

    /// <summary>
    /// 获取用户安全详情
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.Read)]
    public async Task<UserSecurityDetailDto?> GetUserSecurityDetailAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == user.BasicId, cancellationToken);
        return security is null
            ? null
            : UserSecurityApplicationMapper.ToDetailDto(security, user, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建用户安全分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>用户安全分页请求</returns>
    private static BasicAppPRDto BuildUserSecurityPageRequest(UserSecurityPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (input.UserId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSecurity.UserId), input.UserId.Value);
        }

        if (input.IsLocked.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSecurity.IsLocked), input.IsLocked.Value);
        }

        if (input.TwoFactorEnabled.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSecurity.TwoFactorEnabled), input.TwoFactorEnabled.Value);
        }

        if (input.TwoFactorMethod.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSecurity.TwoFactorMethod), input.TwoFactorMethod.Value);
        }

        if (input.EmailVerified.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSecurity.EmailVerified), input.EmailVerified.Value);
        }

        if (input.PhoneVerified.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSecurity.PhoneVerified), input.PhoneVerified.Value);
        }

        if (input.AllowMultiLogin.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysUserSecurity.AllowMultiLogin), input.AllowMultiLogin.Value);
        }

        request.Conditions.AddSort(nameof(SysUserSecurity.IsLocked), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysUserSecurity.LastFailedLoginTime), SortDirection.Descending, 1);
        request.Conditions.AddSort(nameof(SysUserSecurity.CreatedTime), SortDirection.Descending, 2);
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
    private static void ValidatePageInput(UserSecurityPageQueryDto input)
    {
        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        if (input.TwoFactorMethod.HasValue)
        {
            ValidateEnum(input.TwoFactorMethod.Value, nameof(input.TwoFactorMethod));
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
