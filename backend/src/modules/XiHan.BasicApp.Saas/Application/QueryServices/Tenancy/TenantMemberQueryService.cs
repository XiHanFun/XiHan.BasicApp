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
/// 租户成员查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户成员")]
public sealed class TenantMemberQueryService(ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, ITenantMemberQueryService
{
    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 获取租户成员分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Read)]
    public async Task<PageResultDtoBase<TenantMemberListItemDto>> GetTenantMemberPageAsync(TenantMemberPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildTenantMemberPageRequest(input);
        var members = await _tenantUserRepository.GetPagedAsync(request, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        return members.Map(member => TenantMemberApplicationMapper.ToListItemDto(member, now));
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
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysTenantUser.DisplayName),
                nameof(SysTenantUser.InviteRemark),
                nameof(SysTenantUser.Remark));
        }

        if (input.UserId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenantUser.UserId), input.UserId.Value);
        }

        if (input.MemberType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenantUser.MemberType), input.MemberType.Value);
        }

        if (input.InviteStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenantUser.InviteStatus), input.InviteStatus.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenantUser.Status), input.Status.Value);
        }

        if (input.ExpirationTimeStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenantUser.ExpirationTime), input.ExpirationTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ExpirationTimeEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTenantUser.ExpirationTime), input.ExpirationTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort(nameof(SysTenantUser.MemberType), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysTenantUser.CreatedTime), SortDirection.Descending, 1);
        return request;
    }
}
