// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 访问策略查询应用服务接口
/// </summary>
public interface IAccessPolicyQueryService : IApplicationService
{
    /// <summary>
    /// 访问策略分页列表（当前层）
    /// </summary>
    Task<PageResultDtoBase<AccessPolicyListItemDto>> GetAccessPolicyPageAsync(AccessPolicyPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 访问策略详情
    /// </summary>
    Task<AccessPolicyDetailDto?> GetAccessPolicyDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 访问策略能用的条件属性
    /// </summary>
    Task<List<AccessPolicyAttributeDto>> GetAccessPolicyAttributesAsync();

    /// <summary>
    /// 试运行一条策略
    /// </summary>
    Task<AccessPolicyTrialResultDto> GetAccessPolicyTrialAsync(AccessPolicyTrialInputDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 当前的自动封禁（只在平台）
    /// </summary>
    Task<List<LoginBanDto>> GetLoginBansAsync(CancellationToken cancellationToken = default);
}
