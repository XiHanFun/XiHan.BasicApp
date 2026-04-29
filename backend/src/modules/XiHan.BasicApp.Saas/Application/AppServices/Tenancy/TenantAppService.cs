#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantAppService
// Guid:ed0a68f3-7ab5-4ebe-947d-08ef1d83fedc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 租户命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户")]
public sealed class TenantAppService(
    ITenantRepository tenantRepository,
    ICurrentUser currentUser)
    : SaasApplicationService, ITenantAppService
{
    /// <summary>
    /// 租户仓储
    /// </summary>
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    /// <summary>
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Create)]
    public async Task<TenantDetailDto> CreateTenantAsync(TenantCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var tenantCode = input.TenantCode.Trim();
        if (await _tenantRepository.ExistsTenantCodeAsync(tenantCode, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("租户编码已存在。");
        }

        var domain = NormalizeNullable(input.Domain);
        await EnsureDomainAvailableAsync(domain, null, cancellationToken);

        var tenant = new SysTenant
        {
            TenantCode = tenantCode,
            TenantName = input.TenantName.Trim(),
            TenantShortName = NormalizeNullable(input.TenantShortName),
            Logo = NormalizeNullable(input.Logo),
            Domain = domain,
            EditionId = input.EditionId,
            IsolationMode = input.IsolationMode,
            ExpireTime = input.ExpireTime,
            UserLimit = input.UserLimit,
            StorageLimit = input.StorageLimit,
            TenantStatus = TenantStatus.Normal,
            Sort = input.Sort,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedTenant = await _tenantRepository.AddAsync(tenant, cancellationToken);
        return TenantApplicationMapper.ToDetailDto(savedTenant, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 更新租户基础资料
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Update)]
    public async Task<TenantDetailDto> UpdateTenantAsync(TenantUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var tenant = await GetTenantOrThrowAsync(input.BasicId, cancellationToken);
        var domain = NormalizeNullable(input.Domain);
        await EnsureDomainAvailableAsync(domain, tenant.BasicId, cancellationToken);

        tenant.TenantName = input.TenantName.Trim();
        tenant.TenantShortName = NormalizeNullable(input.TenantShortName);
        tenant.Logo = NormalizeNullable(input.Logo);
        tenant.Domain = domain;
        tenant.EditionId = input.EditionId;
        tenant.IsolationMode = input.IsolationMode;
        tenant.ExpireTime = input.ExpireTime;
        tenant.UserLimit = input.UserLimit;
        tenant.StorageLimit = input.StorageLimit;
        tenant.Sort = input.Sort;
        tenant.Remark = NormalizeNullable(input.Remark);

        var savedTenant = await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        return TenantApplicationMapper.ToDetailDto(savedTenant, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 更新租户状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Status)]
    public async Task<TenantDetailDto> UpdateTenantStatusAsync(TenantStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户主键必须大于 0。");
        }

        ValidateEnum(input.TenantStatus, nameof(input.TenantStatus));

        var tenant = await GetTenantOrThrowAsync(input.BasicId, cancellationToken);
        tenant.ChangeStatus(input.TenantStatus, _currentUser.UserId, NormalizeNullable(input.Reason));

        var savedTenant = await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        return TenantApplicationMapper.ToDetailDto(savedTenant, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 获取租户，不存在时抛出异常
    /// </summary>
    /// <param name="id">租户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户实体</returns>
    private async Task<SysTenant> GetTenantOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "租户主键必须大于 0。");
        }

        return await _tenantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户不存在。");
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    /// <param name="input">创建参数</param>
    private static void ValidateCreateInput(TenantCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantName);
        ValidateCommonInput(input.IsolationMode, input.EditionId, input.UserLimit, input.StorageLimit);
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateUpdateInput(TenantUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantName);
        ValidateCommonInput(input.IsolationMode, input.EditionId, input.UserLimit, input.StorageLimit);
    }

    /// <summary>
    /// 校验通用租户资料
    /// </summary>
    /// <param name="isolationMode">隔离模式</param>
    /// <param name="editionId">版本/套餐主键</param>
    /// <param name="userLimit">用户数限制</param>
    /// <param name="storageLimit">存储空间限制</param>
    private static void ValidateCommonInput(TenantIsolationMode isolationMode, long? editionId, int? userLimit, long? storageLimit)
    {
        ValidateEnum(isolationMode, nameof(isolationMode));

        if (editionId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(editionId), "版本/套餐主键必须大于 0。");
        }

        if (userLimit is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userLimit), "用户数限制不能小于 0。");
        }

        if (storageLimit is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storageLimit), "存储空间限制不能小于 0。");
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">枚举值</param>
    /// <param name="paramName">参数名</param>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 确认域名可用
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="excludeId">排除的租户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task EnsureDomainAvailableAsync(string? domain, long? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return;
        }

        var existingTenant = await _tenantRepository.GetByDomainAsync(domain, cancellationToken);
        if (existingTenant is not null && (!excludeId.HasValue || existingTenant.BasicId != excludeId.Value))
        {
            throw new InvalidOperationException("租户域名已存在。");
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>规范化后的字符串</returns>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

}
