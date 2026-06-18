#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityDomainService
// Guid:4b7d6f24-c4dd-4464-ad49-e24eb0fef74c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    private readonly ICurrentTenant _currentTenant;

    private readonly IDepartmentRepository _departmentRepository;

    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;

    private readonly IPermissionRepository _permissionRepository;

    private readonly IResourceRepository _resourceRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldLevelSecurityDomainService(
        IFieldLevelSecurityRepository fieldLevelSecurityRepository,
        IResourceRepository resourceRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IDepartmentRepository departmentRepository,
        ITenantUserRepository tenantUserRepository,
        ICurrentTenant currentTenant)
    {
        _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
        _resourceRepository = resourceRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _departmentRepository = departmentRepository;
        _tenantUserRepository = tenantUserRepository;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task<FieldLevelSecurityCommandResult> CreateAsync(FieldLevelSecurityCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);

        var fieldName = command.FieldName.Trim();
        var resource = await GetEnabledResourceOrThrowAsync(command.ResourceId, cancellationToken);
        var (targetCode, targetName) = await GetAvailableTargetSummaryOrThrowAsync(command.TargetType, command.TargetId, DateTimeOffset.UtcNow, cancellationToken);
        await EnsurePolicyNotExistsAsync(command.TargetType, command.TargetId, command.ResourceId, fieldName, null, cancellationToken);

        var policy = new SysFieldLevelSecurity
        {
            TargetType = command.TargetType,
            TargetId = command.TargetId,
            ResourceId = command.ResourceId,
            FieldName = fieldName,
            IsReadable = command.IsReadable,
            IsEditable = command.IsEditable,
            MaskStrategy = command.MaskStrategy,
            MaskPattern = NormalizeMaskPattern(command.MaskStrategy, command.MaskPattern),
            Priority = command.Priority,
            Description = NormalizeNullable(command.Description),
            Status = command.Status,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedPolicy = await _fieldLevelSecurityRepository.AddAsync(policy, cancellationToken);
        return new FieldLevelSecurityCommandResult(savedPolicy, resource, targetCode, targetName);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var policy = await GetFieldLevelSecurityOrThrowAsync(id, cancellationToken);
        if (!await _fieldLevelSecurityRepository.DeleteAsync(policy, cancellationToken))
        {
            throw new InvalidOperationException("字段级安全策略删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<FieldLevelSecurityCommandResult> UpdateAsync(FieldLevelSecurityUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);

        var policy = await GetFieldLevelSecurityOrThrowAsync(command.BasicId, cancellationToken);
        var fieldName = command.FieldName.Trim();
        var resource = await GetEnabledResourceOrThrowAsync(command.ResourceId, cancellationToken);
        var (targetCode, targetName) = await GetAvailableTargetSummaryOrThrowAsync(command.TargetType, command.TargetId, DateTimeOffset.UtcNow, cancellationToken);
        await EnsurePolicyNotExistsAsync(command.TargetType, command.TargetId, command.ResourceId, fieldName, policy.BasicId, cancellationToken);

        policy.TargetType = command.TargetType;
        policy.TargetId = command.TargetId;
        policy.ResourceId = command.ResourceId;
        policy.FieldName = fieldName;
        policy.IsReadable = command.IsReadable;
        policy.IsEditable = command.IsEditable;
        policy.MaskStrategy = command.MaskStrategy;
        policy.MaskPattern = NormalizeMaskPattern(command.MaskStrategy, command.MaskPattern);
        policy.Priority = command.Priority;
        policy.Description = NormalizeNullable(command.Description);
        policy.Remark = NormalizeNullable(command.Remark);

        var savedPolicy = await _fieldLevelSecurityRepository.UpdateAsync(policy, cancellationToken);
        return new FieldLevelSecurityCommandResult(savedPolicy, resource, targetCode, targetName);
    }

    /// <inheritdoc />
    public async Task<FieldLevelSecurityCommandResult> UpdateStatusAsync(FieldLevelSecurityStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "字段级安全主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var policy = await GetFieldLevelSecurityOrThrowAsync(command.BasicId, cancellationToken);
        var resource = command.Status == EnableStatus.Enabled
            ? await GetEnabledResourceOrThrowAsync(policy.ResourceId, cancellationToken)
            : await _resourceRepository.GetByIdAsync(policy.ResourceId, cancellationToken);
        var (targetCode, targetName) = command.Status == EnableStatus.Enabled
            ? await GetAvailableTargetSummaryOrThrowAsync(policy.TargetType, policy.TargetId, DateTimeOffset.UtcNow, cancellationToken)
            : await GetTargetSummaryOrDefaultAsync(policy.TargetType, policy.TargetId, cancellationToken);

        policy.Status = command.Status;
        policy.Remark = NormalizeNullable(command.Remark) ?? policy.Remark;

        var savedPolicy = await _fieldLevelSecurityRepository.UpdateAsync(policy, cancellationToken);
        return new FieldLevelSecurityCommandResult(savedPolicy, resource, targetCode, targetName);
    }

    /// <summary>
    /// 规范化脱敏模式
    /// </summary>
    private static string? NormalizeMaskPattern(FieldMaskStrategy maskStrategy, string? maskPattern)
    {
        return maskStrategy == FieldMaskStrategy.None ? null : NormalizeNullable(maskPattern);
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonCommand(
        FieldSecurityTargetType targetType,
        long targetId,
        long resourceId,
        string fieldName,
        bool isReadable,
        bool isEditable,
        FieldMaskStrategy maskStrategy,
        int priority)
    {
        ValidateEnum(targetType, nameof(targetType));
        ValidateEnum(maskStrategy, nameof(maskStrategy));
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        if (targetId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetId), "目标主键必须大于 0。");
        }

        if (resourceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resourceId), "资源主键必须大于 0。");
        }

        if (!isReadable && isEditable)
        {
            throw new InvalidOperationException("不可读字段不能设置为可编辑。");
        }

        if (!isReadable && maskStrategy == FieldMaskStrategy.None)
        {
            throw new InvalidOperationException("不可读字段必须指定脱敏策略。");
        }

        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), "优先级不能小于 0。");
        }
    }

    /// <summary>
    /// 校验创建命令
    /// </summary>
    private static void ValidateCreateCommand(FieldLevelSecurityCreateCommand command)
    {
        ValidateCommonCommand(
            command.TargetType,
            command.TargetId,
            command.ResourceId,
            command.FieldName,
            command.IsReadable,
            command.IsEditable,
            command.MaskStrategy,
            command.Priority);
        ValidateEnum(command.Status, nameof(command.Status));
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
    /// 校验更新命令
    /// </summary>
    private static void ValidateUpdateCommand(FieldLevelSecurityUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "字段级安全主键必须大于 0。");
        }

        ValidateCommonCommand(
            command.TargetType,
            command.TargetId,
            command.ResourceId,
            command.FieldName,
            command.IsReadable,
            command.IsEditable,
            command.MaskStrategy,
            command.Priority);
    }

    /// <summary>
    /// 校验字段级安全策略不存在
    /// </summary>
    private async Task EnsurePolicyNotExistsAsync(
        FieldSecurityTargetType targetType,
        long targetId,
        long resourceId,
        string fieldName,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = excludeId.HasValue
            ? await _fieldLevelSecurityRepository.AnyAsync(
                policy => policy.TargetType == targetType
                    && policy.TargetId == targetId
                    && policy.ResourceId == resourceId
                    && policy.FieldName == fieldName
                    && policy.BasicId != excludeId.Value,
                cancellationToken)
            : await _fieldLevelSecurityRepository.AnyAsync(
                policy => policy.TargetType == targetType
                    && policy.TargetId == targetId
                    && policy.ResourceId == resourceId
                    && policy.FieldName == fieldName,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("字段级安全策略已存在。");
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
            throw new InvalidOperationException("停用部门不能配置字段级安全策略。");
        }

        return (department.DepartmentCode, department.DepartmentName);
    }

    /// <summary>
    /// 获取可用权限目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetAvailablePermissionTargetSummaryOrThrowAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能配置字段级安全策略。");
        }

        return (permission.PermissionCode, permission.PermissionName);
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
            throw new InvalidOperationException("停用角色不能配置字段级安全策略。");
        }

        if ((role.IsGlobal || role.RoleType == RoleType.System) && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台全局角色或系统角色字段级安全仅平台运维态可维护，请切换到平台运维后操作。");
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
            FieldSecurityTargetType.Permission => await GetAvailablePermissionTargetSummaryOrThrowAsync(targetId, cancellationToken),
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
            throw new InvalidOperationException("未接受邀请的租户成员不能配置字段级安全策略。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能配置字段级安全策略。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台管理员成员字段级安全仅平台运维态可维护，请切换到平台运维后操作。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能配置字段级安全策略。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能配置字段级安全策略。");
        }

        return (null, tenantMember.DisplayName);
    }

    /// <summary>
    /// 获取部门目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetDepartmentTargetSummaryOrDefaultAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);
        return department is null ? (null, null) : (department.DepartmentCode, department.DepartmentName);
    }

    /// <summary>
    /// 获取已启用资源，不满足规则时抛出异常
    /// </summary>
    private async Task<SysResource> GetEnabledResourceOrThrowAsync(long resourceId, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByIdAsync(resourceId, cancellationToken)
            ?? throw new InvalidOperationException("资源不存在。");

        if (resource.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用资源不能配置字段级安全策略。");
        }

        return resource;
    }

    /// <summary>
    /// 获取字段级安全策略，不存在时抛出异常
    /// </summary>
    private async Task<SysFieldLevelSecurity> GetFieldLevelSecurityOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "字段级安全主键必须大于 0。");
        }

        return await _fieldLevelSecurityRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("字段级安全策略不存在。");
    }

    /// <summary>
    /// 获取权限目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetPermissionTargetSummaryOrDefaultAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
        return permission is null ? (null, null) : (permission.PermissionCode, permission.PermissionName);
    }

    /// <summary>
    /// 获取角色目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetRoleTargetSummaryOrDefaultAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        return role is null ? (null, null) : (role.RoleCode, role.RoleName);
    }

    /// <summary>
    /// 获取目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetTargetSummaryOrDefaultAsync(FieldSecurityTargetType targetType, long targetId, CancellationToken cancellationToken)
    {
        return targetType switch
        {
            FieldSecurityTargetType.Role => await GetRoleTargetSummaryOrDefaultAsync(targetId, cancellationToken),
            FieldSecurityTargetType.User => await GetTenantMemberTargetSummaryOrDefaultAsync(targetId, cancellationToken),
            FieldSecurityTargetType.Permission => await GetPermissionTargetSummaryOrDefaultAsync(targetId, cancellationToken),
            FieldSecurityTargetType.Department => await GetDepartmentTargetSummaryOrDefaultAsync(targetId, cancellationToken),
            _ => (null, null)
        };
    }

    /// <summary>
    /// 获取租户成员目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetTenantMemberTargetSummaryOrDefaultAsync(long userId, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken);
        return tenantMember is null ? (null, null) : (null, tenantMember.DisplayName);
    }
}
