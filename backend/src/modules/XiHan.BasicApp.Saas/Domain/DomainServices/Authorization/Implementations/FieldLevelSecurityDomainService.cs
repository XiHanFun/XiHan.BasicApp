// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字段级安全领域服务实现
/// </summary>
public sealed class FieldLevelSecurityDomainService
    : IFieldLevelSecurityDomainService
{
    private readonly IFieldSecurityEntityCatalog _catalog;

    private readonly ICurrentTenant _currentTenant;

    private readonly IDepartmentRepository _departmentRepository;

    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldLevelSecurityDomainService(
        IFieldLevelSecurityRepository fieldLevelSecurityRepository,
        IFieldSecurityEntityCatalog catalog,
        IRoleRepository roleRepository,
        IDepartmentRepository departmentRepository,
        ITenantUserRepository tenantUserRepository,
        IUserRepository userRepository,
        ICurrentTenant currentTenant)
    {
        _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
        _catalog = catalog;
        _roleRepository = roleRepository;
        _departmentRepository = departmentRepository;
        _tenantUserRepository = tenantUserRepository;
        _userRepository = userRepository;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 创建字段级安全规则
    /// </summary>
    public async Task<FieldLevelSecurityCommandResult> CreateAsync(FieldLevelSecurityCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEnum(command.Status, nameof(command.Status));

        var definition = NormalizeDefinition(
            command.TargetType,
            command.TargetId,
            command.EntityName,
            command.FieldName,
            command.MaskStrategy,
            command.MaskKeepHead,
            command.MaskKeepTail,
            command.MaskReplacement,
            command.IsEditable);
        var (targetCode, targetName) = await GetAvailableTargetSummaryOrThrowAsync(command.TargetType, command.TargetId, DateTimeOffset.UtcNow, cancellationToken);
        await EnsurePolicyNotExistsAsync(definition, null, cancellationToken);

        var policy = new SysFieldLevelSecurity
        {
            Status = command.Status,
            Remark = NormalizeNullable(command.Remark)
        };
        definition.ApplyTo(policy);

        var savedPolicy = await _fieldLevelSecurityRepository.AddAsync(policy, cancellationToken);
        return new FieldLevelSecurityCommandResult(savedPolicy, targetCode, targetName);
    }

    /// <summary>
    /// 删除字段级安全规则
    /// </summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var policy = await GetMaintainablePolicyOrThrowAsync(id, cancellationToken);
        if (!await _fieldLevelSecurityRepository.DeleteAsync(policy, cancellationToken))
        {
            throw new InvalidOperationException("字段级安全规则删除失败。");
        }
    }

    /// <summary>
    /// 更新字段级安全规则
    /// </summary>
    public async Task<FieldLevelSecurityCommandResult> UpdateAsync(FieldLevelSecurityUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var policy = await GetMaintainablePolicyOrThrowAsync(command.BasicId, cancellationToken);
        var definition = NormalizeDefinition(
            command.TargetType,
            command.TargetId,
            command.EntityName,
            command.FieldName,
            command.MaskStrategy,
            command.MaskKeepHead,
            command.MaskKeepTail,
            command.MaskReplacement,
            command.IsEditable);
        var (targetCode, targetName) = await GetAvailableTargetSummaryOrThrowAsync(command.TargetType, command.TargetId, DateTimeOffset.UtcNow, cancellationToken);
        await EnsurePolicyNotExistsAsync(definition, policy.BasicId, cancellationToken);

        definition.ApplyTo(policy);
        policy.Remark = NormalizeNullable(command.Remark);

        var savedPolicy = await _fieldLevelSecurityRepository.UpdateAsync(policy, cancellationToken);
        return new FieldLevelSecurityCommandResult(savedPolicy, targetCode, targetName);
    }

    /// <summary>
    /// 更新字段级安全规则状态
    /// </summary>
    public async Task<FieldLevelSecurityCommandResult> UpdateStatusAsync(FieldLevelSecurityStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEnum(command.Status, nameof(command.Status));

        var policy = await GetMaintainablePolicyOrThrowAsync(command.BasicId, cancellationToken);
        (string? Code, string? Name) target;
        if (command.Status == EnableStatus.Enabled)
        {
            // 启用前按当前代码重新校验：实体或字段可能已随版本移除，目标可能已停用
            _ = NormalizeDefinition(
                policy.TargetType,
                policy.TargetId,
                policy.EntityName,
                policy.FieldName,
                policy.MaskStrategy,
                policy.MaskKeepHead,
                policy.MaskKeepTail,
                policy.MaskReplacement,
                policy.IsEditable);
            target = await GetAvailableTargetSummaryOrThrowAsync(policy.TargetType, policy.TargetId, DateTimeOffset.UtcNow, cancellationToken);
        }
        else
        {
            target = await GetTargetSummaryOrDefaultAsync(policy.TargetType, policy.TargetId, cancellationToken);
        }

        policy.Status = command.Status;
        policy.Remark = NormalizeNullable(command.Remark) ?? policy.Remark;

        var savedPolicy = await _fieldLevelSecurityRepository.UpdateAsync(policy, cancellationToken);
        return new FieldLevelSecurityCommandResult(savedPolicy, target.Code, target.Name);
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验并规范化规则定义：实体与字段必须在目录里，读取方式要适用于字段类型，参数只随对应方式出现，规则必须收紧点什么
    /// </summary>
    private PolicyDefinition NormalizeDefinition(
        FieldSecurityTargetType targetType,
        long targetId,
        string entityName,
        string fieldName,
        FieldMaskStrategy maskStrategy,
        int? maskKeepHead,
        int? maskKeepTail,
        string? maskReplacement,
        bool isEditable)
    {
        ValidateEnum(targetType, nameof(targetType));
        ValidateEnum(maskStrategy, nameof(maskStrategy));
        if (targetId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetId), "目标主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        var entity = _catalog.Find(entityName.Trim())
            ?? throw new InvalidOperationException($"实体「{entityName.Trim()}」不支持字段安全。");
        var field = entity.FindField(fieldName.Trim())
            ?? throw new InvalidOperationException($"「{entity.DisplayName}」没有字段「{fieldName.Trim()}」。");

        if (!field.IsText && maskStrategy is not (FieldMaskStrategy.None or FieldMaskStrategy.Hidden))
        {
            throw new InvalidOperationException($"「{field.DisplayName}」不是文本字段，只能明文只读或隐藏。");
        }

        if (maskStrategy == FieldMaskStrategy.None && isEditable)
        {
            throw new InvalidOperationException("明文且可编辑的规则什么都没限制，请选择脱敏方式或设为只读。");
        }

        int? keepHead = null;
        int? keepTail = null;
        if (maskStrategy == FieldMaskStrategy.PartialMask)
        {
            keepHead = maskKeepHead ?? throw new InvalidOperationException("部分脱敏必须填写保留前几位。");
            keepTail = maskKeepTail ?? throw new InvalidOperationException("部分脱敏必须填写保留后几位。");
            if (keepHead < 0 || keepTail < 0 || keepHead > SysFieldLevelSecurity.MaxMaskKeep || keepTail > SysFieldLevelSecurity.MaxMaskKeep)
            {
                throw new InvalidOperationException($"保留位数须在 0 到 {SysFieldLevelSecurity.MaxMaskKeep} 之间。");
            }

            if (keepHead + keepTail == 0)
            {
                throw new InvalidOperationException("前后都不保留等同于全部星号，请直接选择全部星号。");
            }
        }

        string? replacement = null;
        if (maskStrategy == FieldMaskStrategy.Redact)
        {
            replacement = NormalizeNullable(maskReplacement) ?? throw new InvalidOperationException("固定文本方式必须填写显示的文字。");
            if (replacement.Length > SysFieldLevelSecurity.MaxMaskReplacementLength)
            {
                throw new InvalidOperationException($"固定文本不能超过 {SysFieldLevelSecurity.MaxMaskReplacementLength} 个字符。");
            }
        }

        return new PolicyDefinition(targetType, targetId, entity.EntityName, field.FieldName, maskStrategy, keepHead, keepTail, replacement, isEditable);
    }

    /// <summary>
    /// 校验字段级安全规则不存在（同一目标、同一字段只能有一条）
    /// </summary>
    private async Task EnsurePolicyNotExistsAsync(PolicyDefinition definition, long? excludeId, CancellationToken cancellationToken)
    {
        var exists = excludeId.HasValue
            ? await _fieldLevelSecurityRepository.AnyAsync(
                policy => policy.TargetType == definition.TargetType
                    && policy.TargetId == definition.TargetId
                    && policy.EntityName == definition.EntityName
                    && policy.FieldName == definition.FieldName
                    && policy.BasicId != excludeId.Value,
                cancellationToken)
            : await _fieldLevelSecurityRepository.AnyAsync(
                policy => policy.TargetType == definition.TargetType
                    && policy.TargetId == definition.TargetId
                    && policy.EntityName == definition.EntityName
                    && policy.FieldName == definition.FieldName,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("该目标在这个字段上已有规则，请直接修改那一条。");
        }
    }

    /// <summary>
    /// 获取可用部门目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetAvailableDepartmentTargetSummaryOrThrowAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        if (department.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用部门不能配置字段级安全规则。");
        }

        return (department.DepartmentCode, department.DepartmentName);
    }

    /// <summary>
    /// 获取可用角色目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetAvailableRoleTargetSummaryOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能配置字段级安全规则。");
        }

        if ((role.IsGlobal || role.RoleType == RoleType.System) && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台全局角色或系统角色的字段级安全仅平台运维态可维护，请切换到平台运维后操作。");
        }

        return (role.RoleCode, role.RoleName);
    }

    /// <summary>
    /// 获取可用目标摘要，不满足规则时抛出异常
    /// </summary>
    private async Task<(string? Code, string? Name)> GetAvailableTargetSummaryOrThrowAsync(
        FieldSecurityTargetType targetType,
        long targetId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return targetType switch
        {
            FieldSecurityTargetType.Role => await GetAvailableRoleTargetSummaryOrThrowAsync(targetId, cancellationToken),
            FieldSecurityTargetType.User => await GetAvailableTenantMemberTargetSummaryOrThrowAsync(targetId, now, cancellationToken),
            FieldSecurityTargetType.Department => await GetAvailableDepartmentTargetSummaryOrThrowAsync(targetId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(targetType), "字段级安全目标类型无效。")
        };
    }

    /// <summary>
    /// 获取可用租户成员目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetAvailableTenantMemberTargetSummaryOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能配置字段级安全规则。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能配置字段级安全规则。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能配置字段级安全规则。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能配置字段级安全规则。");
        }

        return await DescribeUserAsync(userId, tenantMember.DisplayName, cancellationToken);
    }

    /// <summary>
    /// 用户目标摘要：成员名片优先，没填时取账号的姓名、昵称、用户名；编码为用户名（账号跨租户按主键取）
    /// </summary>
    private async Task<(string? Code, string? Name)> DescribeUserAsync(long userId, string? memberDisplayName, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdIgnoreTenantAsync(userId, cancellationToken);
        var name = !string.IsNullOrWhiteSpace(memberDisplayName)
            ? memberDisplayName
            : user is null ? null : user.RealName ?? user.NickName ?? user.UserName;
        return (user?.UserName, name);
    }

    /// <summary>
    /// 获取本上下文可维护的规则：平台规则对所有租户生效，租户看得见、改不了
    /// </summary>
    private async Task<SysFieldLevelSecurity> GetMaintainablePolicyOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "字段级安全主键必须大于 0。");
        }

        var policy = await _fieldLevelSecurityRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("字段级安全规则不存在。");
        if (policy.TenantId != (_currentTenant.Id ?? 0))
        {
            throw new InvalidOperationException("平台规则对所有租户生效，只能在平台维护。");
        }

        return policy;
    }

    /// <summary>
    /// 获取目标摘要（停用时目标可能已不可用，只取名称展示）
    /// </summary>
    private async Task<(string? Code, string? Name)> GetTargetSummaryOrDefaultAsync(FieldSecurityTargetType targetType, long targetId, CancellationToken cancellationToken)
    {
        switch (targetType)
        {
            case FieldSecurityTargetType.Role:
                var role = await _roleRepository.GetByIdAsync(targetId, cancellationToken);
                return role is null ? (null, null) : (role.RoleCode, role.RoleName);
            case FieldSecurityTargetType.User:
                var tenantMember = await _tenantUserRepository.GetMembershipAsync(targetId, cancellationToken);
                return await DescribeUserAsync(targetId, tenantMember?.DisplayName, cancellationToken);
            case FieldSecurityTargetType.Department:
                var department = await _departmentRepository.GetByIdAsync(targetId, cancellationToken);
                return department is null ? (null, null) : (department.DepartmentCode, department.DepartmentName);
            default:
                return (null, null);
        }
    }

    /// <summary>
    /// 校验通过、已规范化的规则定义
    /// </summary>
    private sealed record PolicyDefinition(
        FieldSecurityTargetType TargetType,
        long TargetId,
        string EntityName,
        string FieldName,
        FieldMaskStrategy MaskStrategy,
        int? MaskKeepHead,
        int? MaskKeepTail,
        string? MaskReplacement,
        bool IsEditable)
    {
        public void ApplyTo(SysFieldLevelSecurity policy)
        {
            policy.TargetType = TargetType;
            policy.TargetId = TargetId;
            policy.EntityName = EntityName;
            policy.FieldName = FieldName;
            policy.MaskStrategy = MaskStrategy;
            policy.MaskKeepHead = MaskKeepHead;
            policy.MaskKeepTail = MaskKeepTail;
            policy.MaskReplacement = MaskReplacement;
            policy.IsEditable = IsEditable;
        }
    }
}
