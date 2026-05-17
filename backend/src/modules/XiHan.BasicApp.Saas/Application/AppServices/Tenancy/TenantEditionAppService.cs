#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionAppService
// Guid:8b060026-a796-44ad-b557-ff3b44b99c59
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
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 租户版本命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户版本")]
public sealed class TenantEditionAppService
    : SaasApplicationService, ITenantEditionAppService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantEditionAppService(
        ITenantEditionRepository tenantEditionRepository,
        ITenantEditionPermissionRepository tenantEditionPermissionRepository,
        IPermissionRepository permissionRepository)
    {
        _tenantEditionRepository = tenantEditionRepository;
        _tenantEditionPermissionRepository = tenantEditionPermissionRepository;
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// 租户版本仓储
    /// </summary>
    private readonly ITenantEditionRepository _tenantEditionRepository;

    /// <summary>
    /// 租户版本权限仓储
    /// </summary>
    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 创建租户版本
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Create)]
    public async Task<TenantEditionDetailDto> CreateTenantEditionAsync(TenantEditionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var editionCode = input.EditionCode.Trim();
        if (await _tenantEditionRepository.AnyAsync(edition => edition.EditionCode == editionCode, cancellationToken))
        {
            throw new InvalidOperationException("租户版本编码已存在。");
        }

        if (input.IsDefault)
        {
            EnsureEnabledDefault(input.Status);
            await ClearDefaultEditionsAsync(null, cancellationToken);
        }

        var edition = new SysTenantEdition
        {
            EditionCode = editionCode,
            EditionName = input.EditionName.Trim(),
            Description = NormalizeNullable(input.Description),
            UserLimit = input.UserLimit,
            StorageLimit = input.StorageLimit,
            Price = NormalizePrice(input.Price),
            BillingPeriodMonths = input.BillingPeriodMonths,
            IsFree = input.IsFree,
            IsDefault = input.IsDefault,
            Status = input.Status,
            Sort = input.Sort,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedEdition = await _tenantEditionRepository.AddAsync(edition, cancellationToken);
        return TenantEditionApplicationMapper.ToDetailDto(savedEdition);
    }

    /// <summary>
    /// 更新租户版本
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Update)]
    public async Task<TenantEditionDetailDto> UpdateTenantEditionAsync(TenantEditionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var edition = await GetTenantEditionOrThrowAsync(input.BasicId, cancellationToken);
        if (edition.IsDefault && !input.IsDefault)
        {
            throw new InvalidOperationException("默认租户版本不能直接取消，请设置其他版本为默认版本。");
        }

        if (input.IsDefault)
        {
            EnsureEnabledDefault(edition.Status);
            await ClearDefaultEditionsAsync(edition.BasicId, cancellationToken);
        }

        edition.EditionName = input.EditionName.Trim();
        edition.Description = NormalizeNullable(input.Description);
        edition.UserLimit = input.UserLimit;
        edition.StorageLimit = input.StorageLimit;
        edition.Price = NormalizePrice(input.Price);
        edition.BillingPeriodMonths = input.BillingPeriodMonths;
        edition.IsFree = input.IsFree;
        edition.IsDefault = input.IsDefault;
        edition.Sort = input.Sort;
        edition.Remark = NormalizeNullable(input.Remark);

        var savedEdition = await _tenantEditionRepository.UpdateAsync(edition, cancellationToken);
        return TenantEditionApplicationMapper.ToDetailDto(savedEdition);
    }

    /// <summary>
    /// 更新租户版本状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Status)]
    public async Task<TenantEditionDetailDto> UpdateTenantEditionStatusAsync(TenantEditionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户版本主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var edition = await GetTenantEditionOrThrowAsync(input.BasicId, cancellationToken);
        if (edition.IsDefault && input.Status == EnableStatus.Disabled)
        {
            throw new InvalidOperationException("默认租户版本不能被禁用，请先设置其他启用版本为默认版本。");
        }

        edition.Status = input.Status;

        var savedEdition = await _tenantEditionRepository.UpdateAsync(edition, cancellationToken);
        return TenantEditionApplicationMapper.ToDetailDto(savedEdition);
    }

    /// <summary>
    /// 设置默认租户版本
    /// </summary>
    /// <param name="input">默认版本更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Default)]
    public async Task<TenantEditionDetailDto> UpdateDefaultTenantEditionAsync(TenantEditionDefaultUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户版本主键必须大于 0。");
        }

        var edition = await GetTenantEditionOrThrowAsync(input.BasicId, cancellationToken);
        EnsureEnabledDefault(edition.Status);

        await ClearDefaultEditionsAsync(edition.BasicId, cancellationToken);
        edition.IsDefault = true;

        var savedEdition = await _tenantEditionRepository.UpdateAsync(edition, cancellationToken);
        return TenantEditionApplicationMapper.ToDetailDto(savedEdition);
    }

    /// <summary>
    /// 获取租户版本，不存在时抛出异常
    /// </summary>
    /// <param name="id">租户版本主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本实体</returns>
    private async Task<SysTenantEdition> GetTenantEditionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "租户版本主键必须大于 0。");
        }

        return await _tenantEditionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户版本不存在。");
    }

    /// <summary>
    /// 清理其他默认租户版本
    /// </summary>
    /// <param name="excludeId">需要排除的租户版本主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    private Task<bool> ClearDefaultEditionsAsync(long? excludeId, CancellationToken cancellationToken)
    {
        return excludeId.HasValue
            ? _tenantEditionRepository.UpdateAsync(
                edition => new SysTenantEdition { IsDefault = false },
                edition => edition.IsDefault && edition.BasicId != excludeId.Value,
                cancellationToken)
            : _tenantEditionRepository.UpdateAsync(
                edition => new SysTenantEdition { IsDefault = false },
                edition => edition.IsDefault,
                cancellationToken);
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    /// <param name="input">创建参数</param>
    private static void ValidateCreateInput(TenantEditionCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.EditionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.EditionName);
        ValidateCommonInput(input.UserLimit, input.StorageLimit, input.Price, input.BillingPeriodMonths, input.IsFree);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateUpdateInput(TenantEditionUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户版本主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(input.EditionName);
        ValidateCommonInput(input.UserLimit, input.StorageLimit, input.Price, input.BillingPeriodMonths, input.IsFree);
    }

    /// <summary>
    /// 校验通用租户版本资料
    /// </summary>
    /// <param name="userLimit">用户数限制</param>
    /// <param name="storageLimit">存储空间限制</param>
    /// <param name="price">价格</param>
    /// <param name="billingPeriodMonths">计费周期</param>
    /// <param name="isFree">是否免费</param>
    private static void ValidateCommonInput(int? userLimit, long? storageLimit, decimal? price, int? billingPeriodMonths, bool isFree)
    {
        if (userLimit is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userLimit), "用户数限制不能小于 0。");
        }

        if (storageLimit is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storageLimit), "存储空间限制不能小于 0。");
        }

        if (price is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "价格不能小于 0。");
        }

        if (billingPeriodMonths is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(billingPeriodMonths), "计费周期必须大于 0。");
        }

        if (isFree && price is > 0)
        {
            throw new InvalidOperationException("免费版本价格必须为空或 0。");
        }
    }

    /// <summary>
    /// 校验默认版本必须启用
    /// </summary>
    /// <param name="status">状态</param>
    private static void EnsureEnabledDefault(EnableStatus status)
    {
        if (status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("默认租户版本必须处于启用状态。");
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
    /// 规范化价格
    /// </summary>
    /// <param name="price">价格</param>
    /// <returns>规范化后的价格</returns>
    private static decimal? NormalizePrice(decimal? price)
    {
        return price is null ? null : decimal.Round(price.Value, 2, MidpointRounding.AwayFromZero);
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

    #region EditionPermissions

    /// <summary>
    /// 授予租户版本权限
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Grant)]
    public async Task<TenantEditionPermissionDetailDto> GrantTenantEditionPermissionAsync(TenantEditionPermissionGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateGrantInput(input);

        _ = await _tenantEditionRepository.GetByIdAsync(input.EditionId, cancellationToken)
            ?? throw new InvalidOperationException("租户版本不存在。");

        var permission = await GetGrantablePermissionOrThrowAsync(input.PermissionId, cancellationToken);
        if (await _tenantEditionPermissionRepository.AnyAsync(
            editionPermission => editionPermission.EditionId == input.EditionId && editionPermission.PermissionId == input.PermissionId,
            cancellationToken))
        {
            throw new InvalidOperationException("租户版本权限已绑定。");
        }

        var editionPermission = new SysTenantEditionPermission
        {
            EditionId = input.EditionId,
            PermissionId = input.PermissionId,
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedEditionPermission = await _tenantEditionPermissionRepository.AddAsync(editionPermission, cancellationToken);
        return TenantEditionPermissionApplicationMapper.ToDetailDto(savedEditionPermission, permission);
    }

    /// <summary>
    /// 更新租户版本权限状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Update)]
    public async Task<TenantEditionPermissionDetailDto> UpdateTenantEditionPermissionStatusAsync(TenantEditionPermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户版本权限绑定主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var editionPermission = await GetTenantEditionPermissionOrThrowAsync(input.BasicId, cancellationToken);
        var permission = input.Status == ValidityStatus.Valid
            ? await GetGrantablePermissionOrThrowAsync(editionPermission.PermissionId, cancellationToken)
            : await _permissionRepository.GetByIdAsync(editionPermission.PermissionId, cancellationToken);

        editionPermission.Status = input.Status;
        editionPermission.Remark = NormalizeNullable(input.Remark);

        var savedEditionPermission = await _tenantEditionPermissionRepository.UpdateAsync(editionPermission, cancellationToken);
        return TenantEditionPermissionApplicationMapper.ToDetailDto(savedEditionPermission, permission);
    }

    /// <summary>
    /// 撤销租户版本权限
    /// </summary>
    /// <param name="id">租户版本权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Revoke)]
    public async Task RevokeTenantEditionPermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var editionPermission = await GetTenantEditionPermissionOrThrowAsync(id, cancellationToken);
        if (!await _tenantEditionPermissionRepository.DeleteAsync(editionPermission, cancellationToken))
        {
            throw new InvalidOperationException("租户版本权限撤销失败。");
        }
    }

    /// <summary>
    /// 获取租户版本权限绑定，不存在时抛出异常
    /// </summary>
    /// <param name="id">租户版本权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限绑定实体</returns>
    private async Task<SysTenantEditionPermission> GetTenantEditionPermissionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "租户版本权限绑定主键必须大于 0。");
        }

        return await _tenantEditionPermissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户版本权限绑定不存在。");
    }

    /// <summary>
    /// 获取可绑定权限，不满足版本门控规则时抛出异常
    /// </summary>
    /// <param name="permissionId">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限实体</returns>
    private async Task<SysPermission> GetGrantablePermissionOrThrowAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (!permission.IsGlobal)
        {
            throw new InvalidOperationException("租户版本只能绑定平台级全局权限。");
        }

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能绑定到租户版本。");
        }

        return permission;
    }

    /// <summary>
    /// 校验授权参数
    /// </summary>
    /// <param name="input">授权参数</param>
    private static void ValidateGrantInput(TenantEditionPermissionGrantDto input)
    {
        if (input.EditionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户版本主键必须大于 0。");
        }

        if (input.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限主键必须大于 0。");
        }
    }

    #endregion EditionPermissions
}
