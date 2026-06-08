#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionDomainService
// Guid:0371ae5e-75d6-40a8-b52d-9a8c2904a022
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
/// 权限 ABAC 条件领域服务实现
/// </summary>
public sealed class PermissionConditionDomainService
    : IPermissionConditionDomainService
{
    private const int MaxConditionGroups = 5;

    private const int MaxConditionsPerGroup = 10;

    private readonly IPermissionConditionRepository _permissionConditionRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionConditionDomainService(
        IPermissionConditionRepository permissionConditionRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ITenantUserRepository tenantUserRepository,
        ICurrentTenant currentTenant)
    {
        _permissionConditionRepository = permissionConditionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _tenantUserRepository = tenantUserRepository;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task<PermissionConditionCommandResult> CreatePermissionConditionAsync(PermissionConditionCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateConditionCreateInput(command);

        var now = DateTimeOffset.UtcNow;
        await EnsureAuthorizationBindingUsableAsync(command.RolePermissionId, command.UserPermissionId, now, cancellationToken);
        await EnsureConditionLimitsAsync(
            command.RolePermissionId,
            command.UserPermissionId,
            command.ConditionGroup,
            command.AttributeName.Trim(),
            command.ValueType,
            null,
            cancellationToken);

        var condition = new SysPermissionCondition
        {
            RolePermissionId = command.RolePermissionId,
            UserPermissionId = command.UserPermissionId,
            ConditionGroup = command.ConditionGroup,
            AttributeName = command.AttributeName.Trim(),
            Operator = command.Operator,
            IsNegated = command.IsNegated,
            ValueType = command.ValueType,
            ConditionValue = command.ConditionValue.Trim(),
            Description = NormalizeNullable(command.Description),
            Status = command.Status,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedCondition = await _permissionConditionRepository.AddAsync(condition, cancellationToken);
        return new PermissionConditionCommandResult(savedCondition.BasicId);
    }

    /// <inheritdoc />
    public async Task DeletePermissionConditionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var condition = await GetPermissionConditionOrThrowAsync(id, cancellationToken);
        if (!await _permissionConditionRepository.DeleteAsync(condition, cancellationToken))
        {
            throw new InvalidOperationException("权限 ABAC 条件删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<PermissionConditionCommandResult> UpdatePermissionConditionAsync(PermissionConditionUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateConditionUpdateInput(command);

        var now = DateTimeOffset.UtcNow;
        var condition = await GetPermissionConditionOrThrowAsync(command.BasicId, cancellationToken);
        await EnsureAuthorizationBindingUsableAsync(command.RolePermissionId, command.UserPermissionId, now, cancellationToken);
        await EnsureConditionLimitsAsync(
            command.RolePermissionId,
            command.UserPermissionId,
            command.ConditionGroup,
            command.AttributeName.Trim(),
            command.ValueType,
            condition.BasicId,
            cancellationToken);

        condition.RolePermissionId = command.RolePermissionId;
        condition.UserPermissionId = command.UserPermissionId;
        condition.ConditionGroup = command.ConditionGroup;
        condition.AttributeName = command.AttributeName.Trim();
        condition.Operator = command.Operator;
        condition.IsNegated = command.IsNegated;
        condition.ValueType = command.ValueType;
        condition.ConditionValue = command.ConditionValue.Trim();
        condition.Description = NormalizeNullable(command.Description);
        condition.Remark = NormalizeNullable(command.Remark);

        var savedCondition = await _permissionConditionRepository.UpdateAsync(condition, cancellationToken);
        return new PermissionConditionCommandResult(savedCondition.BasicId);
    }

    /// <inheritdoc />
    public async Task<PermissionConditionCommandResult> UpdatePermissionConditionStatusAsync(PermissionConditionStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限 ABAC 条件主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var condition = await GetPermissionConditionOrThrowAsync(command.BasicId, cancellationToken);
        if (command.Status == ValidityStatus.Valid)
        {
            await EnsureAuthorizationBindingUsableAsync(condition.RolePermissionId, condition.UserPermissionId, DateTimeOffset.UtcNow, cancellationToken);
            await EnsureConditionLimitsAsync(
                condition.RolePermissionId,
                condition.UserPermissionId,
                condition.ConditionGroup,
                condition.AttributeName,
                condition.ValueType,
                condition.BasicId,
                cancellationToken);
        }

        condition.Status = command.Status;
        condition.Remark = NormalizeNullable(command.Remark) ?? condition.Remark;

        var savedCondition = await _permissionConditionRepository.UpdateAsync(condition, cancellationToken);
        return new PermissionConditionCommandResult(savedCondition.BasicId);
    }

    private static void EnsureValidity(
        ValidityStatus status,
        DateTimeOffset? effectiveTime,
        DateTimeOffset? expirationTime,
        DateTimeOffset now,
        string message)
    {
        if (status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException(message);
        }

        if (effectiveTime.HasValue && effectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效授权绑定不能配置 ABAC 条件。");
        }

        if (expirationTime.HasValue && expirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期授权绑定不能配置 ABAC 条件。");
        }
    }

    private static bool HasKnownAttributePrefix(string attributeName)
    {
        return attributeName.StartsWith("subject.", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("resource.", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("environment.", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateBindingIds(long? rolePermissionId, long? userPermissionId)
    {
        if (rolePermissionId.HasValue == userPermissionId.HasValue)
        {
            throw new InvalidOperationException("ABAC 条件必须且只能绑定到角色权限或用户直授权限中的一种。");
        }

        if (rolePermissionId.HasValue && rolePermissionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rolePermissionId), "角色权限绑定主键必须大于 0。");
        }

        if (userPermissionId.HasValue && userPermissionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userPermissionId), "用户直授权限绑定主键必须大于 0。");
        }
    }

    private static void ValidateConditionCommonInput(
        long? rolePermissionId,
        long? userPermissionId,
        int conditionGroup,
        string attributeName,
        ConditionOperator conditionOperator,
        ConfigDataType valueType,
        string conditionValue,
        string? description,
        string? remark)
    {
        ValidateBindingIds(rolePermissionId, userPermissionId);
        ValidateEnum(conditionOperator, nameof(conditionOperator));
        ValidateEnum(valueType, nameof(valueType));
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionValue);
        ValidateLength(attributeName, 200, nameof(attributeName), "属性名称不能超过 200 个字符。");
        ValidateOptionalLength(description, 500, nameof(description), "条件说明不能超过 500 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");

        if (conditionGroup < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(conditionGroup), "条件分组不能小于 0。");
        }

        if (!HasKnownAttributePrefix(attributeName.Trim()))
        {
            throw new InvalidOperationException("属性名称必须使用 subject./resource./environment. 命名空间前缀。");
        }
    }

    private static void ValidateConditionCreateInput(PermissionConditionCreateCommand command)
    {
        ValidateConditionCommonInput(
            command.RolePermissionId,
            command.UserPermissionId,
            command.ConditionGroup,
            command.AttributeName,
            command.Operator,
            command.ValueType,
            command.ConditionValue,
            command.Description,
            command.Remark);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateConditionUpdateInput(PermissionConditionUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限 ABAC 条件主键必须大于 0。");
        }

        ValidateConditionCommonInput(
            command.RolePermissionId,
            command.UserPermissionId,
            command.ConditionGroup,
            command.AttributeName,
            command.Operator,
            command.ValueType,
            command.ConditionValue,
            command.Description,
            command.Remark);
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private async Task EnsureAuthorizationBindingUsableAsync(
        long? rolePermissionId,
        long? userPermissionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ValidateBindingIds(rolePermissionId, userPermissionId);

        if (rolePermissionId.HasValue)
        {
            var rolePermission = await _rolePermissionRepository.GetByIdAsync(rolePermissionId.Value, cancellationToken)
                ?? throw new InvalidOperationException("角色权限绑定不存在。");

            EnsureValidity(rolePermission.Status, rolePermission.EffectiveTime, rolePermission.ExpirationTime, now, "无效角色权限绑定不能配置 ABAC 条件。");

            var role = await _roleRepository.GetByIdAsync(rolePermission.RoleId, cancellationToken)
                ?? throw new InvalidOperationException("角色不存在。");
            if (role.Status != EnableStatus.Enabled)
            {
                throw new InvalidOperationException("停用角色不能配置 ABAC 条件。");
            }

            var permission = await _permissionRepository.GetByIdAsync(rolePermission.PermissionId, cancellationToken)
                ?? throw new InvalidOperationException("权限不存在。");
            if (permission.Status != EnableStatus.Enabled)
            {
                throw new InvalidOperationException("停用权限不能配置 ABAC 条件。");
            }
        }

        if (userPermissionId.HasValue)
        {
            var userPermission = await _userPermissionRepository.GetByIdAsync(userPermissionId.Value, cancellationToken)
                ?? throw new InvalidOperationException("用户直授权限绑定不存在。");

            EnsureValidity(userPermission.Status, userPermission.EffectiveTime, userPermission.ExpirationTime, now, "无效用户直授权限绑定不能配置 ABAC 条件。");

            _ = await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, cancellationToken);
            var permission = await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken)
                ?? throw new InvalidOperationException("权限不存在。");
            if (permission.Status != EnableStatus.Enabled)
            {
                throw new InvalidOperationException("停用权限不能配置 ABAC 条件。");
            }
        }
    }

    private async Task EnsureConditionLimitsAsync(
        long? rolePermissionId,
        long? userPermissionId,
        int conditionGroup,
        string attributeName,
        ConfigDataType valueType,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        ValidateBindingIds(rolePermissionId, userPermissionId);

        var existingConditions = await GetConditionsByBindingAsync(rolePermissionId, userPermissionId, cancellationToken);
        var comparableConditions = existingConditions
            .Where(condition => !excludeId.HasValue || condition.BasicId != excludeId.Value)
            .ToArray();

        var groupCount = comparableConditions
            .Select(condition => condition.ConditionGroup)
            .Append(conditionGroup)
            .Distinct()
            .Count();
        if (groupCount > MaxConditionGroups)
        {
            throw new InvalidOperationException($"单条授权绑定最多允许 {MaxConditionGroups} 个 ABAC 条件组。");
        }

        var groupItemCount = comparableConditions.Count(condition => condition.ConditionGroup == conditionGroup) + 1;
        if (groupItemCount > MaxConditionsPerGroup)
        {
            throw new InvalidOperationException($"单个 ABAC 条件组最多允许 {MaxConditionsPerGroup} 条条件。");
        }

        var hasValueTypeConflict = comparableConditions.Any(condition =>
            string.Equals(condition.AttributeName.Trim(), attributeName, StringComparison.OrdinalIgnoreCase) &&
            condition.ValueType != valueType);
        if (hasValueTypeConflict)
        {
            throw new InvalidOperationException("同一授权绑定内相同 ABAC 属性必须使用一致的值类型。");
        }
    }

    private async Task<SysTenantUser> GetAssignableTenantMemberOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能配置 ABAC 条件。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能配置 ABAC 条件。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台管理员成员 ABAC 条件仅平台运维态可维护，请切换到平台运维后操作。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能配置 ABAC 条件。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能配置 ABAC 条件。");
        }

        return tenantMember;
    }

    private async Task<IReadOnlyList<SysPermissionCondition>> GetConditionsByBindingAsync(
        long? rolePermissionId,
        long? userPermissionId,
        CancellationToken cancellationToken)
    {
        if (rolePermissionId.HasValue)
        {
            return await _permissionConditionRepository.GetListAsync(
                condition => condition.RolePermissionId == rolePermissionId.Value,
                condition => condition.ConditionGroup,
                cancellationToken);
        }

        return await _permissionConditionRepository.GetListAsync(
            condition => condition.UserPermissionId == userPermissionId!.Value,
            condition => condition.ConditionGroup,
            cancellationToken);
    }

    private async Task<SysPermissionCondition> GetPermissionConditionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限 ABAC 条件主键必须大于 0。");
        }

        return await _permissionConditionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限 ABAC 条件不存在。");
    }
}
