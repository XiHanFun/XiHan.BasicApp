// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
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

    private readonly IUserRepository _userRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    private readonly ITenantProvisionDomainService _tenantProvisionDomainService;

    private readonly ITenantQuotaDomainService _tenantQuotaDomainService;

    private readonly ICurrentTenant _currentTenant;

    private readonly ITenantConnectionSecretProtector _connectionSecretProtector;

    private readonly ITenantConnectionCacheInvalidator _connectionCacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantDomainService(
        ITenantRepository tenantRepository,
        ITenantUserRepository tenantUserRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        ITenantProvisionDomainService tenantProvisionDomainService,
        ITenantQuotaDomainService tenantQuotaDomainService,
        ICurrentTenant currentTenant,
        ITenantConnectionSecretProtector connectionSecretProtector,
        ITenantConnectionCacheInvalidator connectionCacheInvalidator)
    {
        _tenantRepository = tenantRepository;
        _tenantUserRepository = tenantUserRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _tenantProvisionDomainService = tenantProvisionDomainService;
        _tenantQuotaDomainService = tenantQuotaDomainService;
        _currentTenant = currentTenant;
        _connectionSecretProtector = connectionSecretProtector;
        _connectionCacheInvalidator = connectionCacheInvalidator;
    }

    /// <summary>
    /// 创建租户
    /// </summary>
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
            ConfigStatus = ResolveInitialConfigStatus(command.IsolationMode),
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        ApplyConnectionSettings(tenant, command.DatabaseType, command.ConnectionString, requireConnectionString: true);

        // 版本在建租户时定下：未指定则取默认版本（没有默认版本时保持未绑定，门控按未启用处理）
        if (!tenant.EditionId.HasValue)
        {
            _ = await _tenantProvisionDomainService.AssignDefaultEditionAsync(tenant, cancellationToken);
        }

        return new TenantCommandResult(await _tenantRepository.AddAsync(tenant, cancellationToken), DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 添加租户成员（<c>RequiresInvitation</c> 为 true 时落待接受邀请，否则直接生效）
    /// </summary>
    /// <remarks>成员关系是租户自有数据，只在所属租户内维护：成员加入当前租户，立即生效的要占用席位。</remarks>
    public async Task<TenantMemberCommandResult> AddTenantMemberAsync(TenantMemberAddCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var tenantId = RequireTenantContext();
        EnsureId(command.UserId, "用户主键必须大于 0。");
        ValidateEnum(command.MemberType, nameof(command.MemberType));
        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime);
        EnsureMemberTypeAssignable(command.MemberType);

        _ = await _userRepository.GetByIdIgnoreTenantAsync(command.UserId, cancellationToken)
            ?? throw new UserFriendlyException("用户不存在。");

        var existing = await _tenantUserRepository.GetMembershipAsync(tenantId, command.UserId, cancellationToken);
        if (existing is not null)
        {
            throw new UserFriendlyException("该用户已经是本租户成员。");
        }

        var now = DateTimeOffset.UtcNow;
        var member = new SysTenantUser
        {
            TenantId = tenantId,
            UserId = command.UserId,
            MemberType = command.MemberType,
            InviteStatus = command.RequiresInvitation
                ? TenantMemberInviteStatus.Pending
                : TenantMemberInviteStatus.Accepted,
            InvitedBy = command.OperatorUserId,
            InvitedTime = now,
            RespondedTime = command.RequiresInvitation ? null : now,
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            DisplayName = NormalizeNullable(command.DisplayName),
            InviteRemark = NormalizeNullable(command.InviteRemark),
            Remark = NormalizeNullable(command.Remark),
            Status = ValidityStatus.Valid
        };

        if (OccupiesSeat(member, now))
        {
            await _tenantQuotaDomainService.EnsureSeatQuotaAsync(1, cancellationToken);
        }

        return new TenantMemberCommandResult(await _tenantUserRepository.AddAsync(member, cancellationToken), now);
    }

    /// <summary>
    /// 支持人员入驻：把平台账号以支持成员身份加入指定租户
    /// </summary>
    /// <remarks>平台侧操作，只在平台上下文执行；成员行属于目标租户，切入该租户写入。支持人员不占席位。</remarks>
    public async Task<TenantMemberCommandResult> AddTenantSupportMemberAsync(TenantSupportMemberAddCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        RequirePlatformContext("支持人员的入驻与离场只能在平台执行。");
        EnsureId(command.UserId, "用户主键必须大于 0。");
        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime);

        var tenant = await GetTenantOrThrowAsync(command.TenantId, cancellationToken);
        var user = await _userRepository.GetByIdIgnoreTenantAsync(command.UserId, cancellationToken)
            ?? throw new UserFriendlyException("用户不存在。");
        if (user.TenantId != 0)
        {
            throw new InvalidOperationException("只有平台账号才能作为支持人员入驻租户。");
        }

        using var tenantScope = _currentTenant.Change(tenant.BasicId, tenant.TenantName);
        var existing = await _tenantUserRepository.GetMembershipAsync(tenant.BasicId, command.UserId, cancellationToken);
        if (existing is not null)
        {
            throw new UserFriendlyException("该账号已经是该租户的成员。");
        }

        var now = DateTimeOffset.UtcNow;
        var member = new SysTenantUser
        {
            TenantId = tenant.BasicId,
            UserId = command.UserId,
            MemberType = TenantMemberType.PlatformAdmin,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            InvitedBy = command.OperatorUserId,
            InvitedTime = now,
            RespondedTime = now,
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            Remark = NormalizeNullable(command.Remark),
            Status = ValidityStatus.Valid
        };

        return new TenantMemberCommandResult(await _tenantUserRepository.AddAsync(member, cancellationToken), now);
    }

    /// <summary>
    /// 支持人员离场：撤销平台账号在指定租户的支持成员身份
    /// </summary>
    public async Task RemoveTenantSupportMemberAsync(long tenantId, long memberId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RequirePlatformContext("支持人员的入驻与离场只能在平台执行。");
        var tenant = await GetTenantOrThrowAsync(tenantId, cancellationToken);

        using var tenantScope = _currentTenant.Change(tenant.BasicId, tenant.TenantName);
        var member = await GetTenantMemberOrThrowAsync(memberId, cancellationToken);
        if (member.MemberType != TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台只能移除支持人员，租户自己的成员由租户维护。");
        }

        member.InviteStatus = TenantMemberInviteStatus.Revoked;
        member.Status = ValidityStatus.Invalid;
        member.RespondedTime ??= DateTimeOffset.UtcNow;

        _ = await _tenantUserRepository.UpdateAsync(member, cancellationToken);
    }

    /// <summary>
    /// 所有权转移：把租户所有者身份转给该租户的另一名成员，原所有者改为管理员
    /// </summary>
    /// <remarks>
    /// 平台侧操作，只在平台上下文执行；成员关系与所有者角色的绑定属于目标租户，切入该租户改写。
    /// 所有者身份与所有者角色一起移交：接任者拿到角色，卸任者的这条绑定失效，其它角色原样保留。
    /// 接任者须是已接受邀请、有效、在生效期内的非支持成员；所有者关系始终有效，接任时清掉原有的期限。
    /// </remarks>
    public async Task<TenantOwnerTransferResult> TransferTenantOwnerAsync(TenantOwnerTransferCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        RequirePlatformContext("所有权转移只能在平台执行。");
        EnsureId(command.MemberId, "租户成员主键必须大于 0。");
        var tenant = await GetTenantOrThrowAsync(command.TenantId, cancellationToken);

        using var tenantScope = _currentTenant.Change(tenant.BasicId, tenant.TenantName);
        var now = DateTimeOffset.UtcNow;

        var owners = await _tenantUserRepository.GetListAsync(member => member.MemberType == TenantMemberType.Owner, cancellationToken);
        var previousOwner = owners.Count switch
        {
            0 => throw new UserFriendlyException("该租户还没有所有者，请先初始化管理员。"),
            1 => owners[0],
            _ => throw new InvalidOperationException("租户有多名所有者，数据不一致。")
        };

        var newOwner = await GetTenantMemberOrThrowAsync(command.MemberId, cancellationToken);
        if (newOwner.BasicId == previousOwner.BasicId)
        {
            throw new UserFriendlyException("该成员已经是所有者。");
        }

        if (newOwner.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new UserFriendlyException("支持人员不能成为租户所有者。");
        }

        if (!IsInEffect(newOwner, now))
        {
            throw new UserFriendlyException("只有已接受邀请、有效且在生效期内的成员才能成为所有者。");
        }

        var ownerRole = (await _roleRepository.GetListAsync(
                role => role.TenantId == tenant.BasicId && role.RoleCode == SaasRoleCodes.TenantOwner && role.RoleType == RoleType.System,
                cancellationToken))
            .SingleOrDefault()
            ?? throw new InvalidOperationException("租户缺少所有者角色，数据不一致。");

        previousOwner.MemberType = TenantMemberType.Admin;
        newOwner.MemberType = TenantMemberType.Owner;
        newOwner.EffectiveTime = null;
        newOwner.ExpirationTime = null;
        _ = await _tenantUserRepository.UpdateRangeAsync([previousOwner, newOwner], cancellationToken);

        await MoveOwnerRoleBindingAsync(ownerRole, previousOwner.UserId, newOwner.UserId, cancellationToken);

        return new TenantOwnerTransferResult(previousOwner, newOwner, now);
    }

    /// <summary>
    /// 删除租户（软删，要求租户已停用）
    /// </summary>
    public async Task DeleteTenantAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await GetTenantOrThrowAsync(id, cancellationToken);

        // 停用前置：删除会让租户从所有列表消失，先停用是一次可撤销的确认，也保证在此之前会话/定时任务已按停用语义收口
        if (tenant.TenantStatus is not (TenantStatus.Disabled or TenantStatus.Suspended))
        {
            throw new UserFriendlyException("请先停用或暂停租户，再执行删除。");
        }

        await _tenantRepository.SoftDeleteAsync(tenant, cancellationToken);

        // 库隔离租户的运行时连接按租户缓存，删除后必须失效，否则残留连接仍可被解析出来
        _connectionCacheInvalidator.Invalidate(tenant.BasicId);
    }

    /// <summary>
    /// 撤销租户成员
    /// </summary>
    public async Task DeleteTenantMemberAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = RequireTenantContext();
        var member = await GetTenantMemberOrThrowAsync(id, cancellationToken);
        EnsureOwnerStaysAccepted(member, TenantMemberInviteStatus.Revoked);

        member.InviteStatus = TenantMemberInviteStatus.Revoked;
        member.Status = ValidityStatus.Invalid;
        member.RespondedTime ??= DateTimeOffset.UtcNow;

        _ = await _tenantUserRepository.UpdateAsync(member, cancellationToken);
    }

    /// <summary>
    /// 更新租户
    /// </summary>
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
        tenant.ExpirationTime = command.ExpirationTime;
        tenant.UserLimit = command.UserLimit;
        tenant.StorageLimit = command.StorageLimit;
        tenant.Sort = command.Sort;
        tenant.Remark = NormalizeNullable(command.Remark);

        // 连接串留空表示保持不变；连接可能变更，更新后失效运行时连接缓存
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

    /// <summary>
    /// 更新租户成员
    /// </summary>
    public async Task<TenantMemberCommandResult> UpdateTenantMemberAsync(TenantMemberUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateMemberUpdateCommand(command);
        _ = RequireTenantContext();
        var member = await GetTenantMemberOrThrowAsync(command.BasicId, cancellationToken);
        EnsureNotSupportMember(member);
        EnsureOwnerCanBeChanged(member, command.MemberType);
        if (command.MemberType != member.MemberType)
        {
            EnsureMemberTypeAssignable(command.MemberType);
        }

        var now = DateTimeOffset.UtcNow;
        if (member.MemberType == TenantMemberType.Owner
            && (command.EffectiveTime > now || command.ExpirationTime is not null))
        {
            throw new InvalidOperationException("租户所有者的成员关系始终有效，不能设置生效期限。");
        }

        var occupiedBefore = OccupiesSeat(member, now);
        member.MemberType = command.MemberType;
        member.EffectiveTime = command.EffectiveTime;
        member.ExpirationTime = command.ExpirationTime;
        member.DisplayName = NormalizeNullable(command.DisplayName);
        member.InviteRemark = NormalizeNullable(command.InviteRemark);
        member.Remark = NormalizeNullable(command.Remark);
        await EnsureSeatWhenStartsOccupyingAsync(occupiedBefore, member, now, cancellationToken);

        return new TenantMemberCommandResult(await _tenantUserRepository.UpdateAsync(member, cancellationToken), DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 更新租户成员邀请状态
    /// </summary>
    public async Task<TenantMemberCommandResult> UpdateTenantMemberInviteStatusAsync(TenantMemberInviteStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "租户成员主键必须大于 0。");
        ValidateEnum(command.InviteStatus, nameof(command.InviteStatus));

        _ = RequireTenantContext();
        var member = await GetTenantMemberOrThrowAsync(command.BasicId, cancellationToken);
        EnsureNotSupportMember(member);
        EnsureOwnerStaysAccepted(member, command.InviteStatus);

        var now = DateTimeOffset.UtcNow;
        var occupiedBefore = OccupiesSeat(member, now);
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

        await EnsureSeatWhenStartsOccupyingAsync(occupiedBefore, member, now, cancellationToken);

        return new TenantMemberCommandResult(await _tenantUserRepository.UpdateAsync(member, cancellationToken), DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 更新租户成员状态
    /// </summary>
    public async Task<TenantMemberCommandResult> UpdateTenantMemberStatusAsync(TenantMemberStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "租户成员主键必须大于 0。");
        ValidateEnum(command.Status, nameof(command.Status));

        _ = RequireTenantContext();
        var member = await GetTenantMemberOrThrowAsync(command.BasicId, cancellationToken);
        EnsureNotSupportMember(member);
        if (member.MemberType == TenantMemberType.Owner && command.Status == ValidityStatus.Invalid)
        {
            throw new InvalidOperationException("租户所有者成员关系不能直接停用。");
        }

        var now = DateTimeOffset.UtcNow;
        var occupiedBefore = OccupiesSeat(member, now);
        member.Status = command.Status;
        member.Remark = NormalizeNullable(command.Remark);
        await EnsureSeatWhenStartsOccupyingAsync(occupiedBefore, member, now, cancellationToken);

        return new TenantMemberCommandResult(await _tenantUserRepository.UpdateAsync(member, cancellationToken), DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 更新租户状态
    /// </summary>
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
    /// 解析新建租户的初始配置状态
    /// </summary>
    /// <remarks>
    /// 只有库隔离租户需要额外开库建表（<see cref="ITenantDatabaseInitializer"/>），保持待配置直到初始化成功；
    /// 其余隔离模式建完即可用，直接置为已配置——登录链路以 <c>Configured</c> 为准入条件。
    /// </remarks>
    /// <param name="isolationMode">隔离模式</param>
    /// <returns>初始配置状态</returns>
    private static TenantConfigStatus ResolveInitialConfigStatus(TenantIsolationMode isolationMode)
    {
        return isolationMode == TenantIsolationMode.Database
            ? TenantConfigStatus.Pending
            : TenantConfigStatus.Configured;
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

    private static void ValidateCommonInput(long? editionId, int? userLimit, long? storageLimit)
    {
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
        ValidateEnum(command.IsolationMode, nameof(command.IsolationMode));
        if (command.IsolationMode == TenantIsolationMode.Schema)
        {
            throw new UserFriendlyException("暂不支持 Schema 隔离，请选择字段隔离或库隔离。");
        }

        ValidateCommonInput(command.EditionId, command.UserLimit, command.StorageLimit);
    }

    private static void ValidateUpdateCommand(TenantUpdateCommand command)
    {
        EnsureId(command.BasicId, "租户主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TenantName);
        ValidateCommonInput(command.EditionId, command.UserLimit, command.StorageLimit);
    }

    private static void EnsureOwnerCanBeChanged(SysTenantUser member, TenantMemberType newMemberType)
    {
        if (member.MemberType == TenantMemberType.Owner && newMemberType != TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者成员类型不能直接变更，请由平台转移所有权。");
        }
    }

    /// <summary>
    /// 所有者关系始终是已接受状态：撤销、过期、退回待接受或拒绝都会让租户失去所有者
    /// </summary>
    private static void EnsureOwnerStaysAccepted(SysTenantUser member, TenantMemberInviteStatus newInviteStatus)
    {
        if (member.MemberType == TenantMemberType.Owner && newInviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("租户所有者的成员关系不能撤销、过期或改为未接受，请由平台转移所有权。");
        }
    }

    /// <summary>
    /// 成员类型能否由租户直接指派：所有者只由开通与所有权转移产生，支持人员只由平台入驻
    /// </summary>
    private static void EnsureMemberTypeAssignable(TenantMemberType memberType)
    {
        if (memberType is TenantMemberType.Owner or TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("所有者只能由开通管理员或平台转移所有权产生，支持人员只能由平台入驻。");
        }
    }

    /// <summary>
    /// 成员关系此刻是否生效：已接受、有效、在生效期内
    /// </summary>
    private static bool IsInEffect(SysTenantUser member, DateTimeOffset now)
    {
        return member.InviteStatus == TenantMemberInviteStatus.Accepted
            && member.Status == ValidityStatus.Valid
            && (member.EffectiveTime is null || member.EffectiveTime <= now)
            && (member.ExpirationTime is null || member.ExpirationTime > now);
    }

    /// <summary>
    /// 所有者角色随所有者移交：卸任者的绑定失效，接任者复用历史绑定或新建，都从现在起生效
    /// </summary>
    private async Task MoveOwnerRoleBindingAsync(SysRole ownerRole, long previousUserId, long newUserId, CancellationToken cancellationToken)
    {
        var bindings = await _userRoleRepository.GetListAsync(
            userRole => userRole.RoleId == ownerRole.BasicId && (userRole.UserId == previousUserId || userRole.UserId == newUserId),
            cancellationToken);

        var updating = new List<SysUserRole>();
        foreach (var binding in bindings.Where(binding => binding.UserId == previousUserId && binding.Status == ValidityStatus.Valid))
        {
            binding.Status = ValidityStatus.Invalid;
            binding.Remark = "所有权转移：卸任";
            updating.Add(binding);
        }

        var incoming = bindings.FirstOrDefault(binding => binding.UserId == newUserId);
        if (incoming is null)
        {
            _ = await _userRoleRepository.AddAsync(new SysUserRole
            {
                UserId = newUserId,
                RoleId = ownerRole.BasicId,
                Status = ValidityStatus.Valid,
                GrantReason = "所有权转移"
            }, cancellationToken);
        }
        else
        {
            incoming.Status = ValidityStatus.Valid;
            incoming.EffectiveTime = null;
            incoming.ExpirationTime = null;
            incoming.GrantReason = "所有权转移";
            updating.Add(incoming);
        }

        if (updating.Count > 0)
        {
            _ = await _userRoleRepository.UpdateRangeAsync(updating, cancellationToken);
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

    /// <summary>
    /// 取当前租户的成员关系（成员关系严格隔离，只取得到当前上下文的行）
    /// </summary>
    private async Task<SysTenantUser> GetTenantMemberOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "租户成员主键必须大于 0。");
        return await _tenantUserRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户成员不存在。");
    }

    /// <summary>
    /// 成员关系是租户自有数据，只在所属租户内维护；平台只做支持人员入驻与离场、所有权转移
    /// </summary>
    /// <returns>当前租户主键</returns>
    private long RequireTenantContext()
    {
        return _currentTenant.IsPlatformOperation()
            ? throw new InvalidOperationException("租户成员只能在所属租户内维护，平台只负责支持人员的入驻与离场、所有权转移。")
            : _currentTenant.Id!.Value;
    }

    /// <summary>
    /// 平台侧操作（支持人员入驻与离场、所有权转移）只在平台上下文执行
    /// </summary>
    private void RequirePlatformContext(string message)
    {
        if (!_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 支持人员由平台入驻与离场，租户不改其身份与有效期，只能移除其访问
    /// </summary>
    private static void EnsureNotSupportMember(SysTenantUser member)
    {
        if (member.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("支持人员由平台入驻与离场，租户只能移除其访问。");
        }
    }

    /// <summary>
    /// 成员关系是否占用席位：已接受、有效、在生效期内；支持人员不占席位（与席位统计同口径）
    /// </summary>
    private static bool OccupiesSeat(SysTenantUser member, DateTimeOffset now)
    {
        return member.MemberType != TenantMemberType.PlatformAdmin
            && member.InviteStatus == TenantMemberInviteStatus.Accepted
            && member.Status == ValidityStatus.Valid
            && (member.EffectiveTime is null || member.EffectiveTime <= now)
            && (member.ExpirationTime is null || member.ExpirationTime > now);
    }

    /// <summary>
    /// 变更让成员关系开始占用席位时校验席位额度
    /// </summary>
    private async Task EnsureSeatWhenStartsOccupyingAsync(bool occupiedBefore, SysTenantUser member, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!occupiedBefore && OccupiesSeat(member, now))
        {
            await _tenantQuotaDomainService.EnsureSeatQuotaAsync(1, cancellationToken);
        }
    }
}
