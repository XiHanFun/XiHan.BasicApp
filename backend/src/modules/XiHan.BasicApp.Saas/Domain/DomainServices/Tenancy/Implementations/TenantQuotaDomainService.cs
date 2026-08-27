// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户配额领域服务实现
/// </summary>
public sealed class TenantQuotaDomainService
    : ITenantQuotaDomainService
{
    /// <summary>
    /// 每 MB 字节数：套餐存储上限以 MB 表达，文件占用以字节统计，比较前需换算到同一单位
    /// </summary>
    private const long BytesPerMegabyte = 1024L * 1024L;

    private readonly ICurrentTenant _currentTenant;

    private readonly IFileRepository _fileRepository;

    private readonly ITenantEditionRepository _tenantEditionRepository;

    private readonly ITenantRepository _tenantRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantQuotaDomainService(
        ITenantRepository tenantRepository,
        ITenantEditionRepository tenantEditionRepository,
        ITenantUserRepository tenantUserRepository,
        IFileRepository fileRepository,
        ICurrentTenant currentTenant)
    {
        _tenantRepository = tenantRepository;
        _tenantEditionRepository = tenantEditionRepository;
        _tenantUserRepository = tenantUserRepository;
        _fileRepository = fileRepository;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 校验当前租户新增席位后是否仍在配额内
    /// </summary>
    public async Task EnsureSeatQuotaAsync(int increment, CancellationToken cancellationToken = default)
    {
        if (increment <= 0)
        {
            return;
        }

        var tenantId = ResolveConstrainedTenantId();
        if (tenantId is null)
        {
            return;
        }

        // 先解析上限：不限时直接放行，省掉一次用量统计——这是高频路径，不做无谓查询
        var (userLimit, _) = await ResolveEffectiveLimitsAsync(tenantId.Value, cancellationToken);
        if (userLimit is not { } limit)
        {
            return;
        }

        var used = (await _tenantUserRepository.CountActiveMembersByTenantIdsAsync(
            [tenantId.Value], DateTimeOffset.UtcNow, cancellationToken)).GetValueOrDefault(tenantId.Value);
        if (used + increment > limit)
        {
            throw new InvalidOperationException(
                $"租户席位已达上限（已用 {used} / 上限 {limit}），无法继续新增成员。请升级套餐或调整该租户的席位上限。");
        }
    }

    /// <summary>
    /// 校验当前租户新增存储后是否仍在配额内
    /// </summary>
    public async Task EnsureStorageQuotaAsync(long incrementBytes, CancellationToken cancellationToken = default)
    {
        if (incrementBytes <= 0)
        {
            return;
        }

        var tenantId = ResolveConstrainedTenantId();
        if (tenantId is null)
        {
            return;
        }

        // 同席位校验：先看上限，不限则不统计用量。存储校验不查席位数，两个维度互不牵连
        var (_, storageLimit) = await ResolveEffectiveLimitsAsync(tenantId.Value, cancellationToken);
        if (storageLimit is not { } limitMegabytes)
        {
            return;
        }

        var usedBytes = (await _fileRepository.SumUsedStorageByTenantIdsAsync(
            [tenantId.Value], cancellationToken)).GetValueOrDefault(tenantId.Value);
        if (usedBytes + incrementBytes > limitMegabytes * BytesPerMegabyte)
        {
            throw new InvalidOperationException(
                $"租户存储空间不足（已用 {FormatMegabytes(usedBytes)} MB / 上限 {limitMegabytes} MB），" +
                $"本次上传需要 {FormatMegabytes(incrementBytes)} MB。请清理文件或升级套餐。");
        }
    }

    /// <summary>
    /// 批量获取租户配额快照
    /// </summary>
    public async Task<IReadOnlyDictionary<long, TenantQuotaSnapshot>> GetQuotaSnapshotsAsync(IReadOnlyCollection<long> tenantIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantIds);
        cancellationToken.ThrowIfCancellationRequested();

        var ids = tenantIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, TenantQuotaSnapshot>();
        }

        var tenants = await _tenantRepository.GetByIdsAsync(ids, cancellationToken);
        if (tenants.Count == 0)
        {
            return new Dictionary<long, TenantQuotaSnapshot>();
        }

        // 套餐是上限的回落来源：租户自身未设值时用套餐值，故只取被引用到的套餐
        var editionIds = tenants
            .Where(tenant => tenant.EditionId.HasValue)
            .Select(tenant => tenant.EditionId!.Value)
            .Distinct()
            .ToList();
        Dictionary<long, SysTenantEdition> editionMap = editionIds.Count == 0
            ? []
            : (await _tenantEditionRepository.GetByIdsAsync(editionIds, cancellationToken))
                .ToDictionary(edition => edition.BasicId);

        // 用量两次分组查询拿全，不按租户逐个统计
        var presentIds = tenants.Select(tenant => tenant.BasicId).ToList();
        var seatMap = await _tenantUserRepository.CountActiveMembersByTenantIdsAsync(presentIds, DateTimeOffset.UtcNow, cancellationToken);
        var storageMap = await _fileRepository.SumUsedStorageByTenantIdsAsync(presentIds, cancellationToken);

        return tenants.ToDictionary(
            tenant => tenant.BasicId,
            tenant =>
            {
                var edition = tenant.EditionId.HasValue
                    ? editionMap.GetValueOrDefault(tenant.EditionId.Value)
                    : null;

                return new TenantQuotaSnapshot(
                    tenant.BasicId,
                    tenant.UserLimit ?? edition?.UserLimit,
                    seatMap.GetValueOrDefault(tenant.BasicId),
                    tenant.StorageLimit ?? edition?.StorageLimit,
                    storageMap.GetValueOrDefault(tenant.BasicId));
            });
    }

    /// <summary>
    /// 解析受配额约束的当前租户主键；平台运维态返回 null 表示不受约束
    /// </summary>
    /// <remarks>
    /// 平台运维态无租户上下文、不归属任何租户，自然不受租户配额约束；
    /// 平台管理员切入租户后 Id 已是该租户，走正常校验。
    /// </remarks>
    private long? ResolveConstrainedTenantId()
    {
        return _currentTenant.IsPlatformOperation() ? null : _currentTenant.Id!.Value;
    }

    /// <summary>
    /// 解析单个租户的生效上限（租户级覆盖套餐级，均为空表示不限）
    /// </summary>
    private async Task<(int? UserLimit, long? StorageLimit)> ResolveEffectiveLimitsAsync(long tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return (null, null);
        }

        // 两个维度都已在租户级设值时无需回落，省掉一次套餐查询
        if (tenant.UserLimit is not null && tenant.StorageLimit is not null)
        {
            return (tenant.UserLimit, tenant.StorageLimit);
        }

        var edition = tenant.EditionId.HasValue
            ? await _tenantEditionRepository.GetByIdAsync(tenant.EditionId.Value, cancellationToken)
            : null;

        return (tenant.UserLimit ?? edition?.UserLimit, tenant.StorageLimit ?? edition?.StorageLimit);
    }

    /// <summary>
    /// 字节换算为 MB 文本（最多两位小数，供错误提示阅读）
    /// </summary>
    private static string FormatMegabytes(long bytes)
    {
        return (bytes / (double)BytesPerMegabyte).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
