#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SessionRoleQueryService
// Guid:ec618980-a6b4-45b8-bca4-6b802bee6178
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
/// 会话角色查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "会话角色")]
public sealed class SessionRoleQueryService(
    ISessionRoleRepository sessionRoleRepository,
    IUserSessionRepository userSessionRepository,
    IRoleRepository roleRepository,
    IUserRepository userRepository)
    : SaasApplicationService, ISessionRoleQueryService
{
    /// <summary>
    /// 会话角色仓储
    /// </summary>
    private readonly ISessionRoleRepository _sessionRoleRepository = sessionRoleRepository;

    /// <summary>
    /// 用户会话仓储
    /// </summary>
    private readonly IUserSessionRepository _userSessionRepository = userSessionRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 获取会话角色分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话角色分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.SessionRole.Read)]
    public async Task<PageResultDtoBase<SessionRoleListItemDto>> GetSessionRolePageAsync(SessionRolePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildSessionRolePageRequest(input);
        var sessionRolePage = await _sessionRoleRepository.GetPagedAsync(request, cancellationToken);
        if (sessionRolePage.Items.Count == 0)
        {
            return new PageResultDtoBase<SessionRoleListItemDto>([], sessionRolePage.Page);
        }

        var now = DateTimeOffset.UtcNow;
        var sessionMap = await BuildSessionMapAsync(sessionRolePage.Items.Select(sessionRole => sessionRole.SessionId), cancellationToken);
        var roleMap = await BuildRoleMapAsync(sessionRolePage.Items.Select(sessionRole => sessionRole.RoleId), cancellationToken);
        var userMap = await BuildUserMapAsync(sessionMap.Values.Select(session => session.UserId), cancellationToken);
        var items = sessionRolePage.Items
            .Select(sessionRole =>
            {
                var session = sessionMap.GetValueOrDefault(sessionRole.SessionId);
                var user = session is null ? null : userMap.GetValueOrDefault(session.UserId);
                return SessionRoleApplicationMapper.ToListItemDto(
                    sessionRole,
                    session,
                    roleMap.GetValueOrDefault(sessionRole.RoleId),
                    user,
                    now);
            })
            .ToList();

        return new PageResultDtoBase<SessionRoleListItemDto>(items, sessionRolePage.Page);
    }

    /// <summary>
    /// 获取会话角色详情
    /// </summary>
    /// <param name="id">会话角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话角色详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.SessionRole.Read)]
    public async Task<SessionRoleDetailDto?> GetSessionRoleDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "会话角色主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sessionRole = await _sessionRoleRepository.GetByIdAsync(id, cancellationToken);
        if (sessionRole is null)
        {
            return null;
        }

        var session = await _userSessionRepository.GetByIdAsync(sessionRole.SessionId, cancellationToken);
        var role = await _roleRepository.GetByIdAsync(sessionRole.RoleId, cancellationToken);
        var user = session is null ? null : await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        return SessionRoleApplicationMapper.ToDetailDto(sessionRole, session, role, user, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建会话角色分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>会话角色分页请求</returns>
    private static BasicAppPRDto BuildSessionRolePageRequest(SessionRolePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (input.SessionId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysSessionRole.SessionId), input.SessionId.Value);
        }

        if (input.RoleId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysSessionRole.RoleId), input.RoleId.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysSessionRole.Status), input.Status.Value);
        }

        if (input.ActivatedAtStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysSessionRole.ActivatedAt), input.ActivatedAtStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ActivatedAtEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysSessionRole.ActivatedAt), input.ActivatedAtEnd.Value, QueryOperator.LessThanOrEqual);
        }

        if (input.ExpiresAtStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysSessionRole.ExpiresAt), input.ExpiresAtStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ExpiresAtEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysSessionRole.ExpiresAt), input.ExpiresAtEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort(nameof(SysSessionRole.ActivatedAt), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysSessionRole.CreatedTime), SortDirection.Descending, 1);
        return request;
    }

    /// <summary>
    /// 构建会话映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysUserSession>> BuildSessionMapAsync(IEnumerable<long> sessionIds, CancellationToken cancellationToken)
    {
        var ids = sessionIds
            .Where(sessionId => sessionId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysUserSession>();
        }

        var sessions = await _userSessionRepository.GetByIdsAsync(ids, cancellationToken);
        return sessions.ToDictionary(session => session.BasicId);
    }

    /// <summary>
    /// 构建角色映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysRole>> BuildRoleMapAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds
            .Where(roleId => roleId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysRole>();
        }

        var roles = await _roleRepository.GetByIdsAsync(ids, cancellationToken);
        return roles.ToDictionary(role => role.BasicId);
    }

    /// <summary>
    /// 构建用户映射
    /// </summary>
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
    private static void ValidatePageInput(SessionRolePageQueryDto input)
    {
        ValidateOptionalId(input.SessionId, nameof(input.SessionId), "会话主键必须大于 0。");
        ValidateOptionalId(input.RoleId, nameof(input.RoleId), "角色主键必须大于 0。");
        if (input.Status.HasValue)
        {
            ValidateEnum(input.Status.Value, nameof(input.Status));
        }

        ValidateRange(input.ActivatedAtStart, input.ActivatedAtEnd, nameof(input.ActivatedAtStart), "激活开始时间不能晚于结束时间。");
        ValidateRange(input.ExpiresAtStart, input.ExpiresAtEnd, nameof(input.ExpiresAtStart), "过期开始时间不能晚于结束时间。");
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
            throw new ArgumentOutOfRangeException(paramName, value, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验时间范围
    /// </summary>
    private static void ValidateRange(DateTimeOffset? start, DateTimeOffset? end, string paramName, string message)
    {
        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }
}
