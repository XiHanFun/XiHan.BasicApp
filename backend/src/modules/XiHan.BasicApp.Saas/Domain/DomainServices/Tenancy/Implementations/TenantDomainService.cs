// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户领域服务实现
/// </summary>
public sealed class TenantDomainService
    : ITenantDomainService
{
    private readonly ITenantRepository _tenantRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly ITenantProvisionDomainService _tenantProvisionDomainService;

    private readonly ICurrentTenant _currentTenant;

    private readonly ITenantConnectionSecretProtector _connectionSecretProtector;

    private readonly ITenantConnectionCacheInvalidator _connectionCacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantDomainService(
        ITenantRepository tenantRepository,
        ITenantUserRepository tenantUserRepository,
        ITenantProvisionDomainService tenantProvisionDomainService,
        ICurrentTenant currentTenant,
        ITenantConnectionSecretProtector connectionSecretProtector,
        ITenantConnectionCacheInvalidator connectionCacheInvalidator)
    {
        _tenantRepository = tenantRepository;
        _tenantUserRepository = tenantUserRepository;
        _tenantProvisionDomainService = tenantProvisionDomainService;
        _currentTenant = currentTenant;
        _connectionSecretProtector = connectionSecretProtector;
        _connectionCacheInvalidator = connectionCacheInvalidator;
    }

    /// <inheritdoc />
    public async Task<TenantCommandResult> CreateTenantAsync(TenantCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);
        var tenantCode = command.TenantCode.Trim();
        if (await _tenantRepository.ExistsTenantCodeAsync(tenantCode, cancellationToken: cancellationToken))
        {
            throw new UserFriendlyException(new ResourceLocalizableString("Errors", "Tenant.CodeAlreadyExists"), "租户编码已存在。");
        }

        var domain = NormalizeNullable(command.Domain);
        await EnsureDomainAvailableAsync(domain, null, cancellationToken);

        var tenant = new SysTenant
        {
            TenantCode = tenantCode,
            TenantName = command.TenantName.Trim(),
            TenantShortName = NormalizeNullable(command.TenantShortName),
            Logo = NormalizeNullable(command.Logo),
            Domain = domain,
            EditionId = command.EditionId,
            IsolationMode = command.IsolationMode,
            ExpirationTime = command.ExpirationTime,
            UserLimit = command.UserLimit,
            StorageLimit = command.StorageLimit,
            TenantStatus = TenantStatus.Normal,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        ApplyConnectionSettings(tenant, command.DatabaseType, command.ConnectionString, requireConnectionString: true);

        return new TenantCommandResult(await _tenantRepository.AddAsync(tenant, cancellationToken), DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task DeleteTenantMemberAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var member = await GetTenantMemberOrThrowAsync(id, cancellationToken);
        EnsureOwnerCanBeRevoked(member, TenantMemberInviteStatus.Revoked);

        member.InviteStatus = TenantMemberInviteStatus.Revoked;
        member.Status = ValidityStatus.Invalid;
        member.RespondedTime ??= DateTimeOffset.UtcNow;

        _ = await _tenantUserRepository.UpdateAsync(member, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TenantCommandResult> UpdateTenantAsync(TenantUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);
        var tenant = await GetTenantOrThrowAsync(command.BasicId, cancellationToken);
        var domain = NormalizeNullable(command.Domain);
        await EnsureDomainAvailableAsync(domain, tenant.BasicId, cancellationToken);

        var previousEditionId = tenant.EditionId;

        tenant.TenantName = command.TenantName.Trim();
        tenant.TenantShortName = NormalizeNullable(command.TenantShortName);
        tenant.Logo = NormalizeNullable(command.Logo);
        tenant.Domain = domain;
        tenant.EditionId = command.EditionId;
        tenant.IsolationMode = command.IsolationMode;
        tenant.ExpirationTime = command.ExpirationTime;
        tenant.UserLimit = command.UserLimit;
        tenant.StorageLimit = command.StorageLimit;
        tenant.Sort = command.Sort;
        tenant.Remark = NormalizeNullable(command.Remark);

        // 连接串留空表示保持不变；隔离/连接可能变更，更新后失效运行时连接缓存
        ApplyConnectionSettings(tenant, command.DatabaseType, command.ConnectionString, requireConnectionString: false);

        var updated = await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        _connectionCacheInvalidator.Invalidate(tenant.BasicId);

        // 套餐变更（含降级）：回收超出新版本白名单的存量角色/用户直授权限行（REQ-5.3）
        if (previousEditionId != command.EditionId)
        {
            _ = await _tenantProvisionDomainService.ReconcileTenantAuthorizationWithEditionAsync(updated, cancellationToken);
        }

        return new TenantCommandResult(updated, DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<TenantMemberCommandResult> UpdateTenantMemberAsync(TenantMemberUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateMemberUpdateCommand(command);
        var member = await GetTenantMemberOrThrowAsync(command.BasicId, cancellationToken);
        EnsureOwnerCanBeChanged(member, command.MemberType);
        EnsurePlatformAdminNotAssigned(command.MemberType);

        member.MemberType = command.MemberType;
        member.EffectiveTime = command.EffectiveTime;
        member.ExpirationTime = command.ExpirationTime;
        member.DisplayName = NormalizeNullable(command.DisplayName);
        member.InviteRemark = NormalizeNullable(command.InviteRemark);
        member.Remark = NormalizeNullable(command.Remark);

        return new TenantMemberCommandResult(await _tenantUserRepository.UpdateAsync(member, cancellationToken), DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<TenantMemberCommandResult> UpdateTenantMemberInviteStatusAsync(TenantMemberInviteStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "租户成员主键必须大于 0。");
        ValidateEnum(command.InviteStatus, nameof(command.InviteStatus));

        var member = await GetTenantMemberOrThrowAsync(command.BasicId, cancellationToken);
        EnsureOwnerCanBeRevoked(member, command.InviteStatus);

        member.InviteStatus = command.InviteStatus;
        member.InviteRemark = NormalizeNullable(command.InviteRemark);

        if (command.InviteStatus is TenantMemberInviteStatus.Accepted or TenantMemberInviteStatus.Rejected)
        {
            member.RespondedTime = DateTimeOffset.UtcNow;
        }

        if (command.InviteStatus is TenantMemberInviteStatus.Revoked or TenantMemberInviteStatus.Expired)
        {
            member.Status = ValidityStatus.Invalid;
            member.RespondedTime ??= DateTimeOffset.UtcNow;
        }

        return new TenantMemberCommandResult(await _tenantUserRepository.UpdateAsync(member, cancellationToken), DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<TenantMemberCommandResult> UpdateTenantMemberStatusAsync(TenantMemberStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "租户成员主键必须大于 0。");
        ValidateEnum(command.Status, nameof(command.Status));

        var member = await GetTenantMemberOrThrowAsync(command.BasicId, cancellationToken);
        if (member.MemberType == TenantMemberType.Owner && command.Status == ValidityStatus.Invalid)
        {
            throw new InvalidOperationException("租户所有者成员关系不能直接停用。");
        }

        member.Status = command.Status;
        member.Remark = NormalizeNullable(command.Remark);

        return new TenantMemberCommandResult(await _tenantUserRepository.UpdateAsync(member, cancellationToken), DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<TenantCommandResult> UpdateTenantStatusAsync(TenantStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "租户主键必须大于 0。");
        ValidateEnum(command.TenantStatus, nameof(command.TenantStatus));

        var tenant = await GetTenantOrThrowAsync(command.BasicId, cancellationToken);
        tenant.ChangeStatus(command.TenantStatus, command.OperatorUserId, NormalizeNullable(command.Reason));

        return new TenantCommandResult(await _tenantRepository.UpdateAsync(tenant, cancellationToken), DateTimeOffset.UtcNow);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 应用库隔离连接设置：库隔离校验数据库类型/连接串并加密落库；非库隔离清空相关字段
    /// </summary>
    /// <param name="tenant">租户实体（IsolationMode 须已赋值）</param>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="connectionString">连接字符串明文（留空表示保持不变）</param>
    /// <param name="requireConnectionString">是否强制要求提供连接串（创建库隔离租户时为 true）</param>
    private void ApplyConnectionSettings(SysTenant tenant, TenantDatabaseType? databaseType, string? connectionString, bool requireConnectionString)
    {
        if (tenant.IsolationMode != TenantIsolationMode.Database)
        {
            // 非库隔离：清空库隔离相关字段，避免残留脏连接
            tenant.DatabaseType = null;
            tenant.ConnectionString = null;
            tenant.IsConnectionStringEncrypted = false;
            return;
        }

        if (databaseType is null)
        {
            throw new InvalidOperationException("库隔离（Database）租户必须指定数据库类型。");
        }

        tenant.DatabaseType = databaseType;

        var plaintext = NormalizeNullable(connectionString);
        if (plaintext is not null)
        {
            tenant.ConnectionString = _connectionSecretProtector.Protect(plaintext);
            tenant.IsConnectionStringEncrypted = true;
        }
        else if (requireConnectionString || string.IsNullOrWhiteSpace(tenant.ConnectionString))
        {
            throw new InvalidOperationException("库隔离（Database）租户必须提供数据库连接字符串。");
        }
    }

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

    private static void ValidateCreateCommand(TenantCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TenantCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TenantName);
        ValidateCommonInput(command.IsolationMode, command.EditionId, command.UserLimit, command.StorageLimit);
    }

    private static void ValidateUpdateCommand(TenantUpdateCommand command)
    {
        EnsureId(command.BasicId, "租户主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TenantName);
        ValidateCommonInput(command.IsolationMode, command.EditionId, command.UserLimit, command.StorageLimit);
    }

    private static void EnsureOwnerCanBeChanged(SysTenantUser member, TenantMemberType newMemberType)
    {
        if (member.MemberType == TenantMemberType.Owner && newMemberType != TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者成员类型不能直接变更。");
        }
    }

    private static void EnsureOwnerCanBeRevoked(SysTenantUser member, TenantMemberInviteStatus newInviteStatus)
    {
        if (member.MemberType == TenantMemberType.Owner && newInviteStatus is TenantMemberInviteStatus.Revoked or TenantMemberInviteStatus.Expired)
        {
            throw new InvalidOperationException("租户所有者成员关系不能直接撤销或过期。");
        }
    }

    private static void ValidateEffectivePeriod(DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime)
    {
        if (effectiveTime.HasValue && expirationTime.HasValue && expirationTime.Value <= effectiveTime.Value)
        {
            throw new InvalidOperationException("租户成员失效时间必须晚于生效时间。");
        }
    }

    private static void ValidateMemberUpdateCommand(TenantMemberUpdateCommand command)
    {
        EnsureId(command.BasicId, "租户成员主键必须大于 0。");
        ValidateEnum(command.MemberType, nameof(command.MemberType));
        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime);
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

    private async Task<SysTenant> GetTenantOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "租户主键必须大于 0。");
        return await _tenantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户不存在。");
    }

    private void EnsurePlatformAdminNotAssigned(TenantMemberType memberType)
    {
        if (memberType == TenantMemberType.PlatformAdmin && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台管理员成员身份仅平台运维态可分配，请切换到平台运维后操作。");
        }
    }

    private async Task<SysTenantUser> GetTenantMemberOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "租户成员主键必须大于 0。");
        return await _tenantUserRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户成员不存在。");
    }
}
