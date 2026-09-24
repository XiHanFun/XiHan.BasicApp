// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.MultiTenancy.Abstractions;

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

    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantMemberQueryService(
        ITenantUserRepository tenantUserRepository,
        IUserRepository userRepository,
        ICurrentTenant currentTenant)
    {
        _tenantUserRepository = tenantUserRepository;
        _userRepository = userRepository;
        _currentTenant = currentTenant;
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

        // 成员关系属于租户：租户只看本租户；平台不带租户时看平台自己的（0 号），带租户时切入该租户查看
        var tenantId = ResolveMemberTenantId(input.TenantId);
        using var tenantScope = _currentTenant.Change(tenantId);
        var request = BuildTenantMemberPageRequest(input, tenantId);
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

        // 读共享口径下能读到 0 号行，按当前上下文精确比对
        var member = await _tenantUserRepository.GetByIdAsync(id, cancellationToken);
        return member is null || member.TenantId != (_currentTenant.Id ?? 0)
            ? null
            : TenantMemberApplicationMapper.ToDetailDto(member, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建租户成员分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>租户成员分页请求</returns>
    /// <summary>
    /// 确定要查看哪个租户的成员
    /// </summary>
    private long ResolveMemberTenantId(long? requestedTenantId)
    {
        if (_currentTenant.IsPlatformOperation())
        {
            return requestedTenantId ?? 0;
        }

        var currentTenantId = _currentTenant.Id!.Value;
        return requestedTenantId is null || requestedTenantId == currentTenantId
            ? currentTenantId
            : throw new InvalidOperationException("只能查看本租户的成员。");
    }

    private static BasicAppPRDto BuildTenantMemberPageRequest(TenantMemberPageQueryDto input, long tenantId)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        // 显式按租户精确过滤：租户上下文的读共享口径会一并放行 0 号行
        request.Conditions.AddFilter((SysTenantUser member) => member.TenantId, tenantId);

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
