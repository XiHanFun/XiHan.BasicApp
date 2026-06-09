#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionDomainService
// Guid:7dff0167-cbc6-4604-9436-b4e7e2abac3f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户版本领域服务实现
/// </summary>
public sealed class TenantEditionDomainService
    : ITenantEditionDomainService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantEditionDomainService(
        ITenantEditionRepository tenantEditionRepository,
        ITenantEditionPermissionRepository tenantEditionPermissionRepository,
        IPermissionRepository permissionRepository)
    {
        _tenantEditionRepository = tenantEditionRepository;
        _tenantEditionPermissionRepository = tenantEditionPermissionRepository;
        _permissionRepository = permissionRepository;
    }

    private readonly IPermissionRepository _permissionRepository;
    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository;
    private readonly ITenantEditionRepository _tenantEditionRepository;

    /// <inheritdoc />
    public async Task<TenantEditionCommandResult> CreateTenantEditionAsync(TenantEditionCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);
        var editionCode = command.EditionCode.Trim();
        if (await _tenantEditionRepository.AnyAsync(edition => edition.EditionCode == editionCode, cancellationToken))
        {
            throw new InvalidOperationException("租户版本编码已存在。");
        }

        if (command.IsDefault)
        {
            EnsureEnabledDefault(command.Status);
            await ClearDefaultEditionsAsync(null, cancellationToken);
        }

        var edition = new SysTenantEdition
        {
            EditionCode = editionCode,
            EditionName = command.EditionName.Trim(),
            Description = NormalizeNullable(command.Description),
            UserLimit = command.UserLimit,
            StorageLimit = command.StorageLimit,
            Price = NormalizePrice(command.Price),
            BillingPeriodMonths = command.BillingPeriodMonths,
            IsFree = command.IsFree,
            IsDefault = command.IsDefault,
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        return new TenantEditionCommandResult(await _tenantEditionRepository.AddAsync(edition, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<TenantEditionPermissionCommandResult> GrantTenantEditionPermissionAsync(TenantEditionPermissionGrantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateGrantCommand(command);
        _ = await _tenantEditionRepository.GetByIdAsync(command.EditionId, cancellationToken)
            ?? throw new InvalidOperationException("租户版本不存在。");

        var permission = await GetGrantablePermissionOrThrowAsync(command.PermissionId, cancellationToken);
        if (await _tenantEditionPermissionRepository.AnyAsync(
            editionPermission => editionPermission.EditionId == command.EditionId && editionPermission.PermissionId == command.PermissionId,
            cancellationToken))
        {
            throw new InvalidOperationException("租户版本权限已绑定。");
        }

        var editionPermission = new SysTenantEditionPermission
        {
            EditionId = command.EditionId,
            PermissionId = command.PermissionId,
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(command.Remark)
        };

        return new TenantEditionPermissionCommandResult(
            await _tenantEditionPermissionRepository.AddAsync(editionPermission, cancellationToken),
            permission);
    }

    /// <inheritdoc />
    public async Task RevokeTenantEditionPermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var editionPermission = await GetTenantEditionPermissionOrThrowAsync(id, cancellationToken);
        if (!await _tenantEditionPermissionRepository.DeleteAsync(editionPermission, cancellationToken))
        {
            throw new InvalidOperationException("租户版本权限撤销失败。");
        }
    }

    /// <inheritdoc />
    public async Task<TenantEditionCommandResult> UpdateDefaultTenantEditionAsync(TenantEditionDefaultChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "租户版本主键必须大于 0。");
        var edition = await GetTenantEditionOrThrowAsync(command.BasicId, cancellationToken);
        EnsureEnabledDefault(edition.Status);

        await ClearDefaultEditionsAsync(edition.BasicId, cancellationToken);
        edition.IsDefault = true;

        return new TenantEditionCommandResult(await _tenantEditionRepository.UpdateAsync(edition, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<TenantEditionCommandResult> UpdateTenantEditionAsync(TenantEditionUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);
        var edition = await GetTenantEditionOrThrowAsync(command.BasicId, cancellationToken);
        if (edition.IsDefault && !command.IsDefault)
        {
            throw new InvalidOperationException("默认租户版本不能直接取消，请设置其他版本为默认版本。");
        }

        if (command.IsDefault)
        {
            EnsureEnabledDefault(edition.Status);
            await ClearDefaultEditionsAsync(edition.BasicId, cancellationToken);
        }

        edition.EditionName = command.EditionName.Trim();
        edition.Description = NormalizeNullable(command.Description);
        edition.UserLimit = command.UserLimit;
        edition.StorageLimit = command.StorageLimit;
        edition.Price = NormalizePrice(command.Price);
        edition.BillingPeriodMonths = command.BillingPeriodMonths;
        edition.IsFree = command.IsFree;
        edition.IsDefault = command.IsDefault;
        edition.Sort = command.Sort;
        edition.Remark = NormalizeNullable(command.Remark);

        return new TenantEditionCommandResult(await _tenantEditionRepository.UpdateAsync(edition, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<TenantEditionPermissionCommandResult> UpdateTenantEditionPermissionStatusAsync(TenantEditionPermissionStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "租户版本权限绑定主键必须大于 0。");
        ValidateEnum(command.Status, nameof(command.Status));

        var editionPermission = await GetTenantEditionPermissionOrThrowAsync(command.BasicId, cancellationToken);
        var permission = command.Status == ValidityStatus.Valid
            ? await GetGrantablePermissionOrThrowAsync(editionPermission.PermissionId, cancellationToken)
            : await _permissionRepository.GetByIdAsync(editionPermission.PermissionId, cancellationToken);

        editionPermission.Status = command.Status;
        editionPermission.Remark = NormalizeNullable(command.Remark);

        return new TenantEditionPermissionCommandResult(
            await _tenantEditionPermissionRepository.UpdateAsync(editionPermission, cancellationToken),
            permission);
    }

    /// <inheritdoc />
    public async Task<TenantEditionCommandResult> UpdateTenantEditionStatusAsync(TenantEditionStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "租户版本主键必须大于 0。");
        ValidateEnum(command.Status, nameof(command.Status));

        var edition = await GetTenantEditionOrThrowAsync(command.BasicId, cancellationToken);
        if (edition.IsDefault && command.Status == EnableStatus.Disabled)
        {
            throw new InvalidOperationException("默认租户版本不能被禁用，请先设置其他启用版本为默认版本。");
        }

        edition.Status = command.Status;
        return new TenantEditionCommandResult(await _tenantEditionRepository.UpdateAsync(edition, cancellationToken));
    }

    private static void EnsureEnabledDefault(EnableStatus status)
    {
        if (status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("默认租户版本必须处于启用状态。");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static decimal? NormalizePrice(decimal? price)
    {
        return price is null ? null : decimal.Round(price.Value, 2, MidpointRounding.AwayFromZero);
    }

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

    private static void ValidateCreateCommand(TenantEditionCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.EditionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.EditionName);
        ValidateCommonInput(command.UserLimit, command.StorageLimit, command.Price, command.BillingPeriodMonths, command.IsFree);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateUpdateCommand(TenantEditionUpdateCommand command)
    {
        EnsureId(command.BasicId, "租户版本主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(command.EditionName);
        ValidateCommonInput(command.UserLimit, command.StorageLimit, command.Price, command.BillingPeriodMonths, command.IsFree);
    }

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

    private async Task<SysTenantEdition> GetTenantEditionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "租户版本主键必须大于 0。");
        return await _tenantEditionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户版本不存在。");
    }

    private static void ValidateGrantCommand(TenantEditionPermissionGrantCommand command)
    {
        EnsureId(command.EditionId, "租户版本主键必须大于 0。");
        EnsureId(command.PermissionId, "权限主键必须大于 0。");
    }

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

    private async Task<SysTenantEditionPermission> GetTenantEditionPermissionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "租户版本权限绑定主键必须大于 0。");
        return await _tenantEditionPermissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户版本权限绑定不存在。");
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }
}
