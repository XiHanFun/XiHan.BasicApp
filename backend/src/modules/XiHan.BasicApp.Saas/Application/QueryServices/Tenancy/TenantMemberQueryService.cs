#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberQueryService
// Guid:8ae86d27-0fea-44a6-a016-45ad24f0e863
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
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
/// 租户成员查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户成员")]
public sealed class TenantMemberQueryService
    : SaasApplicationService, ITenantMemberQueryService
{
    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository;

    /// <summary>
    /// 用户仓储（用于批量解析成员身份）
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantMemberQueryService(
        ITenantUserRepository tenantUserRepository,
        IUserRepository userRepository)
    {
        _tenantUserRepository = tenantUserRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 获取租户成员分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<TenantMemberListItemDto>> GetTenantMemberPageAsync(TenantMemberPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildTenantMemberPageRequest(input);
        var members = await _tenantUserRepository.GetPagedAsync(request, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        // 批量解析成员身份（一次 IN 查询，不做 N+1）。忽略租户过滤：跨租户成员的 SysUser 属于来源租户。
        var users = await _userRepository.GetListByIdsIgnoreTenantAsync(
            [.. members.Items.Select(member => member.UserId)], cancellationToken);
        var userMap = users.ToDictionary(user => user.BasicId);

        return members.Map(member =>
        {
            var dto = TenantMemberApplicationMapper.ToListItemDto(member, now);
            if (userMap.TryGetValue(member.UserId, out var user))
            {
                dto.UserName = user.UserName;
                dto.RealName = user.RealName;
                dto.NickName = user.NickName;
            }

            return dto;
        });
    }

    /// <summary>
    /// 获取租户成员详情
    /// </summary>
    /// <param name="id">租户成员主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Read)]
    public async Task<TenantMemberDetailDto?> GetTenantMemberDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "租户成员主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var member = await _tenantUserRepository.GetByIdAsync(id, cancellationToken);
        return member is null ? null : TenantMemberApplicationMapper.ToDetailDto(member, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建租户成员分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>租户成员分页请求</returns>
    private static BasicAppPRDto BuildTenantMemberPageRequest(TenantMemberPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        // 必须显式按租户过滤：平台管理员没有租户上下文，全局租户过滤器在平台态放行全部，
        // 少了这一刀，「租户详情 → 成员」会把所有租户的成员关系都捞出来。
        if (input.TenantId.HasValue)
        {
            request.Conditions.AddFilter((SysTenantUser member) => member.TenantId, input.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysTenantUser>(
                input.Keyword.Trim(),
                member => member.DisplayName,
                member => member.InviteRemark,
                member => member.Remark);
        }

        if (input.UserId.HasValue)
        {
            request.Conditions.AddFilter((SysTenantUser member) => member.UserId, input.UserId.Value);
        }

        if (input.MemberType.HasValue)
        {
            request.Conditions.AddFilter((SysTenantUser member) => member.MemberType, input.MemberType.Value);
        }

        if (input.InviteStatus.HasValue)
        {
            request.Conditions.AddFilter((SysTenantUser member) => member.InviteStatus, input.InviteStatus.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysTenantUser member) => member.Status, input.Status.Value);
        }

        if (input.ExpirationTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysTenantUser member) => member.ExpirationTime, input.ExpirationTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ExpirationTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysTenantUser member) => member.ExpirationTime, input.ExpirationTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort((SysTenantUser member) => member.MemberType, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysTenantUser member) => member.CreatedTime, SortDirection.Descending, 1);
        return request;
    }
}
