#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionAppService
// Guid:3f388b9d-e804-4f0d-9d73-445ced093f4a
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
/// 权限 ABAC 条件命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限ABAC条件")]
public sealed class PermissionConditionAppService(
    IPermissionConditionRepository permissionConditionRepository,
    IRolePermissionRepository rolePermissionRepository,
    IUserPermissionRepository userPermissionRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IPermissionConditionAppService
{
    private const int MaxConditionGroups = 5;
    private const int MaxConditionsPerGroup = 10;

    /// <summary>
    /// 权限 ABAC 条件仓储
    /// </summary>
    private readonly IPermissionConditionRepository _permissionConditionRepository = permissionConditionRepository;

    /// <summary>
    /// 角色权限仓储
    /// </summary>
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;

    /// <summary>
    /// 用户直授权限仓储
    /// </summary>
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 创建权限 ABAC 条件
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Create)]
    public async Task<PermissionConditionDetailDto> CreatePermissionConditionAsync(PermissionConditionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var now = DateTimeOffset.UtcNow;
        await EnsureAuthorizationBindingUsableAsync(input.RolePermissionId, input.UserPermissionId, now, cancellationToken);
        await EnsureConditionLimitsAsync(
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName.Trim(),
            input.ValueType,
            null,
            cancellationToken);

        var condition = new SysPermissionCondition
        {
            RolePermissionId = input.RolePermissionId,
            UserPermissionId = input.UserPermissionId,
            ConditionGroup = input.ConditionGroup,
            AttributeName = input.AttributeName.Trim(),
            Operator = input.Operator,
            IsNegated = input.IsNegated,
            ValueType = input.ValueType,
            ConditionValue = input.ConditionValue.Trim(),
            Description = NormalizeNullable(input.Description),
            Status = input.Status,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedCondition = await _permissionConditionRepository.AddAsync(condition, cancellationToken);
        return await BuildDetailDtoAsync(savedCondition, cancellationToken);
    }

    /// <summary>
    /// 更新权限 ABAC 条件
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Update)]
    public async Task<PermissionConditionDetailDto> UpdatePermissionConditionAsync(PermissionConditionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var condition = await GetPermissionConditionOrThrowAsync(input.BasicId, cancellationToken);
        await EnsureAuthorizationBindingUsableAsync(input.RolePermissionId, input.UserPermissionId, now, cancellationToken);
        await EnsureConditionLimitsAsync(
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName.Trim(),
            input.ValueType,
            condition.BasicId,
            cancellationToken);

        condition.RolePermissionId = input.RolePermissionId;
        condition.UserPermissionId = input.UserPermissionId;
        condition.ConditionGroup = input.ConditionGroup;
        condition.AttributeName = input.AttributeName.Trim();
        condition.Operator = input.Operator;
        condition.IsNegated = input.IsNegated;
        condition.ValueType = input.ValueType;
        condition.ConditionValue = input.ConditionValue.Trim();
        condition.Description = NormalizeNullable(input.Description);
        condition.Remark = NormalizeNullable(input.Remark);

        var savedCondition = await _permissionConditionRepository.UpdateAsync(condition, cancellationToken);
        return await BuildDetailDtoAsync(savedCondition, cancellationToken);
    }

    /// <summary>
    /// 更新权限 ABAC 条件状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Status)]
    public async Task<PermissionConditionDetailDto> UpdatePermissionConditionStatusAsync(PermissionConditionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限 ABAC 条件主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var condition = await GetPermissionConditionOrThrowAsync(input.BasicId, cancellationToken);
        if (input.Status == ValidityStatus.Valid)
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

        condition.Status = input.Status;
        condition.Remark = NormalizeNullable(input.Remark) ?? condition.Remark;

        var savedCondition = await _permissionConditionRepository.UpdateAsync(condition, cancellationToken);
        return await BuildDetailDtoAsync(savedCondition, cancellationToken);
    }

    /// <summary>
    /// 删除权限 ABAC 条件
    /// </summary>
    /// <param name="id">ABAC 条件主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Delete)]
    public async Task DeletePermissionConditionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var condition = await GetPermissionConditionOrThrowAsync(id, cancellationToken);
        if (!await _permissionConditionRepository.DeleteAsync(condition, cancellationToken))
        {
            throw new InvalidOperationException("权限 ABAC 条件删除失败。");
        }
    }

    /// <summary>
    /// 获取权限 ABAC 条件，不存在时抛出异常
    /// </summary>
    private async Task<SysPermissionCondition> GetPermissionConditionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限 ABAC 条件主键必须大于 0。");
        }

        return await _permissionConditionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限 ABAC 条件不存在。");
    }

    /// <summary>
    /// 校验授权绑定可用
    /// </summary>
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

    /// <summary>
    /// 校验授权有效性
    /// </summary>
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

    /// <summary>
    /// 获取可授权租户成员
    /// </summary>
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

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员 ABAC 条件必须通过平台运维流程维护。");
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

    /// <summary>
    /// 校验 ABAC 条件数量和值类型一致性
    /// </summary>
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

    /// <summary>
    /// 获取授权绑定下的条件集合
    /// </summary>
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

    /// <summary>
    /// 构建详情 DTO
    /// </summary>
    private async Task<PermissionConditionDetailDto> BuildDetailDtoAsync(SysPermissionCondition condition, CancellationToken cancellationToken)
    {
        SysRolePermission? rolePermission = null;
        SysUserPermission? userPermission = null;
        SysPermission? permission = null;
        SysRole? role = null;
        SysTenantUser? tenantMember = null;

        if (condition.RolePermissionId.HasValue)
        {
            rolePermission = await _rolePermissionRepository.GetByIdAsync(condition.RolePermissionId.Value, cancellationToken);
            if (rolePermission is not null)
            {
                role = await _roleRepository.GetByIdAsync(rolePermission.RoleId, cancellationToken);
                permission = await _permissionRepository.GetByIdAsync(rolePermission.PermissionId, cancellationToken);
            }
        }

        if (condition.UserPermissionId.HasValue)
        {
            userPermission = await _userPermissionRepository.GetByIdAsync(condition.UserPermissionId.Value, cancellationToken);
            if (userPermission is not null)
            {
                tenantMember = await _tenantUserRepository.GetMembershipAsync(userPermission.UserId, cancellationToken);
                permission ??= await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken);
            }
        }

        return PermissionConditionApplicationMapper.ToDetailDto(condition, rolePermission, userPermission, permission, role, tenantMember);
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(PermissionConditionCreateDto input)
    {
        ValidateCommonInput(
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName,
            input.Operator,
            input.ValueType,
            input.ConditionValue,
            input.Description,
            input.Remark);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(PermissionConditionUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限 ABAC 条件主键必须大于 0。");
        }

        ValidateCommonInput(
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName,
            input.Operator,
            input.ValueType,
            input.ConditionValue,
            input.Description,
            input.Remark);
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonInput(
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

    /// <summary>
    /// 校验绑定主键
    /// </summary>
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

    /// <summary>
    /// 判断属性命名空间是否有效
    /// </summary>
    private static bool HasKnownAttributePrefix(string attributeName)
    {
        return attributeName.StartsWith("subject.", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("resource.", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("environment.", StringComparison.OrdinalIgnoreCase);
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
    /// 校验字符串长度
    /// </summary>
    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验可空字符串长度
    /// </summary>
    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
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
