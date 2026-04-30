#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityAppService
// Guid:88ff96f5-6968-4021-9c5b-310928906521
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
/// 字段级安全命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "字段级安全")]
public sealed class FieldLevelSecurityAppService(
    IFieldLevelSecurityRepository fieldLevelSecurityRepository,
    IResourceRepository resourceRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IDepartmentRepository departmentRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IFieldLevelSecurityAppService
{
    /// <summary>
    /// 字段级安全仓储
    /// </summary>
    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository = fieldLevelSecurityRepository;

    /// <summary>
    /// 资源仓储
    /// </summary>
    private readonly IResourceRepository _resourceRepository = resourceRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 创建字段级安全策略
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Create)]
    public async Task<FieldLevelSecurityDetailDto> CreateFieldLevelSecurityAsync(FieldLevelSecurityCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var fieldName = input.FieldName.Trim();
        var resource = await GetEnabledResourceOrThrowAsync(input.ResourceId, cancellationToken);
        var (targetCode, targetName) = await GetAvailableTargetSummaryOrThrowAsync(input.TargetType, input.TargetId, DateTimeOffset.UtcNow, cancellationToken);
        await EnsurePolicyNotExistsAsync(input.TargetType, input.TargetId, input.ResourceId, fieldName, null, cancellationToken);

        var policy = new SysFieldLevelSecurity
        {
            TargetType = input.TargetType,
            TargetId = input.TargetId,
            ResourceId = input.ResourceId,
            FieldName = fieldName,
            IsReadable = input.IsReadable,
            IsEditable = input.IsEditable,
            MaskStrategy = input.MaskStrategy,
            MaskPattern = NormalizeMaskPattern(input.MaskStrategy, input.MaskPattern),
            Priority = input.Priority,
            Description = NormalizeNullable(input.Description),
            Status = input.Status,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedPolicy = await _fieldLevelSecurityRepository.AddAsync(policy, cancellationToken);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(savedPolicy, resource, targetCode, targetName);
    }

    /// <summary>
    /// 更新字段级安全策略
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Update)]
    public async Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityAsync(FieldLevelSecurityUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var policy = await GetFieldLevelSecurityOrThrowAsync(input.BasicId, cancellationToken);
        var fieldName = input.FieldName.Trim();
        var resource = await GetEnabledResourceOrThrowAsync(input.ResourceId, cancellationToken);
        var (targetCode, targetName) = await GetAvailableTargetSummaryOrThrowAsync(input.TargetType, input.TargetId, DateTimeOffset.UtcNow, cancellationToken);
        await EnsurePolicyNotExistsAsync(input.TargetType, input.TargetId, input.ResourceId, fieldName, policy.BasicId, cancellationToken);

        policy.TargetType = input.TargetType;
        policy.TargetId = input.TargetId;
        policy.ResourceId = input.ResourceId;
        policy.FieldName = fieldName;
        policy.IsReadable = input.IsReadable;
        policy.IsEditable = input.IsEditable;
        policy.MaskStrategy = input.MaskStrategy;
        policy.MaskPattern = NormalizeMaskPattern(input.MaskStrategy, input.MaskPattern);
        policy.Priority = input.Priority;
        policy.Description = NormalizeNullable(input.Description);
        policy.Remark = NormalizeNullable(input.Remark);

        var savedPolicy = await _fieldLevelSecurityRepository.UpdateAsync(policy, cancellationToken);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(savedPolicy, resource, targetCode, targetName);
    }

    /// <summary>
    /// 更新字段级安全策略状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Status)]
    public async Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityStatusAsync(FieldLevelSecurityStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "字段级安全主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var policy = await GetFieldLevelSecurityOrThrowAsync(input.BasicId, cancellationToken);
        var resource = input.Status == EnableStatus.Enabled
            ? await GetEnabledResourceOrThrowAsync(policy.ResourceId, cancellationToken)
            : await _resourceRepository.GetByIdAsync(policy.ResourceId, cancellationToken);
        var (targetCode, targetName) = input.Status == EnableStatus.Enabled
            ? await GetAvailableTargetSummaryOrThrowAsync(policy.TargetType, policy.TargetId, DateTimeOffset.UtcNow, cancellationToken)
            : await GetTargetSummaryOrDefaultAsync(policy.TargetType, policy.TargetId, cancellationToken);

        policy.Status = input.Status;
        policy.Remark = NormalizeNullable(input.Remark) ?? policy.Remark;

        var savedPolicy = await _fieldLevelSecurityRepository.UpdateAsync(policy, cancellationToken);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(savedPolicy, resource, targetCode, targetName);
    }

    /// <summary>
    /// 删除字段级安全策略
    /// </summary>
    /// <param name="id">字段级安全主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Delete)]
    public async Task DeleteFieldLevelSecurityAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var policy = await GetFieldLevelSecurityOrThrowAsync(id, cancellationToken);
        if (!await _fieldLevelSecurityRepository.DeleteAsync(policy, cancellationToken))
        {
            throw new InvalidOperationException("字段级安全策略删除失败。");
        }
    }

    /// <summary>
    /// 获取字段级安全策略，不存在时抛出异常
    /// </summary>
    /// <param name="id">字段级安全主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全策略</returns>
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
    /// 获取已启用资源，不满足规则时抛出异常
    /// </summary>
    /// <param name="resourceId">资源主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源实体</returns>
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
    /// 获取可用目标摘要，不满足规则时抛出异常
    /// </summary>
    /// <param name="targetType">目标类型</param>
    /// <param name="targetId">目标主键</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>目标摘要</returns>
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

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色字段级安全必须通过平台运维流程维护。");
        }

        return (role.RoleCode, role.RoleName);
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

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员字段级安全必须通过平台运维流程维护。");
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
    /// 获取角色目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetRoleTargetSummaryOrDefaultAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        return role is null ? (null, null) : (role.RoleCode, role.RoleName);
    }

    /// <summary>
    /// 获取租户成员目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetTenantMemberTargetSummaryOrDefaultAsync(long userId, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken);
        return tenantMember is null ? (null, null) : (null, tenantMember.DisplayName);
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
    /// 获取部门目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetDepartmentTargetSummaryOrDefaultAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);
        return department is null ? (null, null) : (department.DepartmentCode, department.DepartmentName);
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
    /// 校验创建参数
    /// </summary>
    /// <param name="input">创建参数</param>
    private static void ValidateCreateInput(FieldLevelSecurityCreateDto input)
    {
        ValidateCommonInput(
            input.TargetType,
            input.TargetId,
            input.ResourceId,
            input.FieldName,
            input.IsReadable,
            input.IsEditable,
            input.MaskStrategy,
            input.Priority);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateUpdateInput(FieldLevelSecurityUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "字段级安全主键必须大于 0。");
        }

        ValidateCommonInput(
            input.TargetType,
            input.TargetId,
            input.ResourceId,
            input.FieldName,
            input.IsReadable,
            input.IsEditable,
            input.MaskStrategy,
            input.Priority);
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonInput(
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
    /// 规范化脱敏模式
    /// </summary>
    /// <param name="maskStrategy">脱敏策略</param>
    /// <param name="maskPattern">脱敏模式</param>
    /// <returns>规范化后的脱敏模式</returns>
    private static string? NormalizeMaskPattern(FieldMaskStrategy maskStrategy, string? maskPattern)
    {
        return maskStrategy == FieldMaskStrategy.None ? null : NormalizeNullable(maskPattern);
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
