#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleAppService
// Guid:ab316ec1-c221-4644-a3e9-c17b6e89e4f6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
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
/// 约束规则命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "约束规则")]
public sealed class ConstraintRuleAppService(
    IConstraintRuleRepository constraintRuleRepository,
    IConstraintRuleItemRepository constraintRuleItemRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IConstraintRuleAppService
{
    /// <summary>
    /// 约束规则仓储
    /// </summary>
    private readonly IConstraintRuleRepository _constraintRuleRepository = constraintRuleRepository;

    /// <summary>
    /// 约束规则项仓储
    /// </summary>
    private readonly IConstraintRuleItemRepository _constraintRuleItemRepository = constraintRuleItemRepository;

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
    /// 创建约束规则
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Create)]
    public async Task<ConstraintRuleDetailDto> CreateConstraintRuleAsync(ConstraintRuleCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateCreateInput(input, now);

        var ruleCode = input.RuleCode.Trim();
        await EnsureRuleCodeNotExistsAsync(ruleCode, null, cancellationToken);
        await ValidateRuleItemsAsync(input.ConstraintType, input.TargetType, input.Items, now, cancellationToken);

        var rule = new SysConstraintRule
        {
            RuleCode = ruleCode,
            RuleName = input.RuleName.Trim(),
            ConstraintType = input.ConstraintType,
            TargetType = input.TargetType,
            Parameters = NormalizeParameters(input.Parameters),
            IsGlobal = false,
            Status = input.Status,
            ViolationAction = input.ViolationAction,
            Description = NormalizeNullable(input.Description),
            Priority = input.Priority,
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedRule = await _constraintRuleRepository.AddAsync(rule, cancellationToken);
        var savedItems = await ReplaceRuleItemsAsync(savedRule.BasicId, input.Items, cancellationToken);
        return await BuildDetailDtoAsync(savedRule, savedItems, now, cancellationToken);
    }

    /// <summary>
    /// 更新约束规则
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Update)]
    public async Task<ConstraintRuleDetailDto> UpdateConstraintRuleAsync(ConstraintRuleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateUpdateInput(input, now);

        var rule = await GetEditableRuleOrThrowAsync(input.BasicId, cancellationToken);
        await ValidateRuleItemsAsync(input.ConstraintType, input.TargetType, input.Items, now, cancellationToken);

        rule.RuleName = input.RuleName.Trim();
        rule.ConstraintType = input.ConstraintType;
        rule.TargetType = input.TargetType;
        rule.Parameters = NormalizeParameters(input.Parameters);
        rule.ViolationAction = input.ViolationAction;
        rule.Description = NormalizeNullable(input.Description);
        rule.Priority = input.Priority;
        rule.EffectiveTime = input.EffectiveTime;
        rule.ExpirationTime = input.ExpirationTime;
        rule.Remark = NormalizeNullable(input.Remark);

        var savedRule = await _constraintRuleRepository.UpdateAsync(rule, cancellationToken);
        var savedItems = await ReplaceRuleItemsAsync(savedRule.BasicId, input.Items, cancellationToken);
        return await BuildDetailDtoAsync(savedRule, savedItems, now, cancellationToken);
    }

    /// <summary>
    /// 更新约束规则状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Status)]
    public async Task<ConstraintRuleDetailDto> UpdateConstraintRuleStatusAsync(ConstraintRuleStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "约束规则主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var now = DateTimeOffset.UtcNow;
        var rule = await GetEditableRuleOrThrowAsync(input.BasicId, cancellationToken);
        var items = await GetRuleItemsAsync(rule.BasicId, cancellationToken);
        if (input.Status == EnableStatus.Enabled)
        {
            ValidateRuleItems(rule.ConstraintType, rule.TargetType, ToInputItems(items));
            await EnsureTargetsUsableAsync(items, now, cancellationToken);
            rule.Enable();
        }
        else
        {
            rule.Disable();
        }

        rule.Remark = NormalizeNullable(input.Remark) ?? rule.Remark;

        var savedRule = await _constraintRuleRepository.UpdateAsync(rule, cancellationToken);
        return await BuildDetailDtoAsync(savedRule, items, now, cancellationToken);
    }

    /// <summary>
    /// 删除约束规则
    /// </summary>
    /// <param name="id">约束规则主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Delete)]
    public async Task DeleteConstraintRuleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rule = await GetEditableRuleOrThrowAsync(id, cancellationToken);
        _ = await _constraintRuleItemRepository.DeleteAsync(item => item.ConstraintRuleId == rule.BasicId, cancellationToken);
        if (!await _constraintRuleRepository.DeleteAsync(rule, cancellationToken))
        {
            throw new InvalidOperationException("约束规则删除失败。");
        }
    }

    /// <summary>
    /// 获取可维护约束规则，不满足规则时抛出异常
    /// </summary>
    private async Task<SysConstraintRule> GetEditableRuleOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "约束规则主键必须大于 0。");
        }

        var rule = await _constraintRuleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("约束规则不存在。");

        EnsureRuleCanBeMaintained(rule);
        return rule;
    }

    /// <summary>
    /// 校验约束规则可由当前服务维护
    /// </summary>
    private static void EnsureRuleCanBeMaintained(SysConstraintRule rule)
    {
        if (rule.IsGlobal)
        {
            throw new InvalidOperationException("平台级全局约束规则必须通过平台运维流程维护。");
        }
    }

    /// <summary>
    /// 校验规则编码不存在
    /// </summary>
    private async Task EnsureRuleCodeNotExistsAsync(string ruleCode, long? excludeId, CancellationToken cancellationToken)
    {
        var exists = excludeId.HasValue
            ? await _constraintRuleRepository.AnyAsync(
                rule => rule.RuleCode == ruleCode && rule.BasicId != excludeId.Value,
                cancellationToken)
            : await _constraintRuleRepository.AnyAsync(rule => rule.RuleCode == ruleCode, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("约束规则编码已存在。");
        }
    }

    /// <summary>
    /// 替换约束规则项
    /// </summary>
    private async Task<IReadOnlyList<SysConstraintRuleItem>> ReplaceRuleItemsAsync(
        long ruleId,
        IReadOnlyList<ConstraintRuleItemInputDto> inputs,
        CancellationToken cancellationToken)
    {
        _ = await _constraintRuleItemRepository.DeleteAsync(item => item.ConstraintRuleId == ruleId, cancellationToken);

        var items = inputs
            .Select(input => new SysConstraintRuleItem
            {
                ConstraintRuleId = ruleId,
                TargetType = input.TargetType,
                TargetId = input.TargetId,
                ConstraintGroup = input.ConstraintGroup,
                Remark = NormalizeNullable(input.Remark)
            })
            .ToArray();

        return await _constraintRuleItemRepository.AddRangeAsync(items, cancellationToken);
    }

    /// <summary>
    /// 获取规则项
    /// </summary>
    private async Task<IReadOnlyList<SysConstraintRuleItem>> GetRuleItemsAsync(long ruleId, CancellationToken cancellationToken)
    {
        return await _constraintRuleItemRepository.GetListAsync(
            item => item.ConstraintRuleId == ruleId,
            item => item.ConstraintGroup,
            cancellationToken);
    }

    /// <summary>
    /// 构建详情 DTO
    /// </summary>
    private async Task<ConstraintRuleDetailDto> BuildDetailDtoAsync(
        SysConstraintRule rule,
        IReadOnlyList<SysConstraintRuleItem> items,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var itemDtos = new List<ConstraintRuleItemDto>(items.Count);
        foreach (var item in items.OrderBy(item => item.ConstraintGroup).ThenBy(item => item.TargetType).ThenBy(item => item.TargetId))
        {
            var summary = await GetTargetSummaryOrDefaultAsync(item.TargetType, item.TargetId, cancellationToken);
            itemDtos.Add(ConstraintRuleApplicationMapper.ToItemDto(item, summary.Code, summary.Name));
        }

        return ConstraintRuleApplicationMapper.ToDetailDto(rule, itemDtos, now);
    }

    /// <summary>
    /// 校验规则项并检查目标可用性
    /// </summary>
    private async Task ValidateRuleItemsAsync(
        ConstraintType constraintType,
        ConstraintTargetType targetType,
        IReadOnlyList<ConstraintRuleItemInputDto> items,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ValidateRuleItems(constraintType, targetType, items);

        foreach (var item in items)
        {
            _ = await GetAvailableTargetSummaryOrThrowAsync(item.TargetType, item.TargetId, now, cancellationToken);
        }
    }

    /// <summary>
    /// 校验规则项结构
    /// </summary>
    private static void ValidateRuleItems(
        ConstraintType constraintType,
        ConstraintTargetType targetType,
        IReadOnlyList<ConstraintRuleItemInputDto> items)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("约束规则至少需要一个规则项。");
        }

        var duplicated = items
            .GroupBy(item => new { item.TargetType, item.TargetId })
            .Any(group => group.Count() > 1);
        if (duplicated)
        {
            throw new InvalidOperationException("约束规则项目标不能重复。");
        }

        foreach (var item in items)
        {
            ValidateEnum(item.TargetType, nameof(item.TargetType));
            if (item.TargetId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(item.TargetId), "规则项目标主键必须大于 0。");
            }

            if (item.ConstraintGroup < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(item.ConstraintGroup), "约束分组不能小于 0。");
            }

            if (constraintType != ConstraintType.Prerequisite && item.TargetType != targetType)
            {
                throw new InvalidOperationException("非先决条件约束的规则项目标类型必须与规则目标类型一致。");
            }
        }

        if (constraintType is ConstraintType.SSD or ConstraintType.DSD or ConstraintType.MutualExclusion && items.Count < 2)
        {
            throw new InvalidOperationException("职责分离或互斥约束至少需要两个目标项。");
        }

        if (constraintType == ConstraintType.Prerequisite)
        {
            if (!items.Any(item => item.ConstraintGroup == 0) || !items.Any(item => item.ConstraintGroup == 1))
            {
                throw new InvalidOperationException("先决条件约束必须同时包含必备项分组 0 和目标项分组 1。");
            }
        }
    }

    /// <summary>
    /// 校验现有规则项目标可用
    /// </summary>
    private async Task EnsureTargetsUsableAsync(IReadOnlyList<SysConstraintRuleItem> items, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            _ = await GetAvailableTargetSummaryOrThrowAsync(item.TargetType, item.TargetId, now, cancellationToken);
        }
    }

    /// <summary>
    /// 获取可用目标摘要，不满足规则时抛出异常
    /// </summary>
    private async Task<(string? Code, string? Name)> GetAvailableTargetSummaryOrThrowAsync(
        ConstraintTargetType targetType,
        long targetId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return targetType switch
        {
            ConstraintTargetType.Role => await GetAvailableRoleTargetSummaryOrThrowAsync(targetId, cancellationToken),
            ConstraintTargetType.Permission => await GetAvailablePermissionTargetSummaryOrThrowAsync(targetId, cancellationToken),
            ConstraintTargetType.User => await GetAvailableTenantMemberTargetSummaryOrThrowAsync(targetId, now, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(targetType), "约束规则目标类型无效。")
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
            throw new InvalidOperationException("停用角色不能配置约束规则。");
        }

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色约束规则必须通过平台运维流程维护。");
        }

        return (role.RoleCode, role.RoleName);
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
            throw new InvalidOperationException("停用权限不能配置约束规则。");
        }

        return (permission.PermissionCode, permission.PermissionName);
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
            throw new InvalidOperationException("未接受邀请的租户成员不能配置约束规则。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能配置约束规则。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员约束规则必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能配置约束规则。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能配置约束规则。");
        }

        return (null, tenantMember.DisplayName);
    }

    /// <summary>
    /// 获取目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetTargetSummaryOrDefaultAsync(ConstraintTargetType targetType, long targetId, CancellationToken cancellationToken)
    {
        return targetType switch
        {
            ConstraintTargetType.Role => await GetRoleTargetSummaryOrDefaultAsync(targetId, cancellationToken),
            ConstraintTargetType.Permission => await GetPermissionTargetSummaryOrDefaultAsync(targetId, cancellationToken),
            ConstraintTargetType.User => await GetTenantMemberTargetSummaryOrDefaultAsync(targetId, cancellationToken),
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
    /// 获取权限目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> GetPermissionTargetSummaryOrDefaultAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
        return permission is null ? (null, null) : (permission.PermissionCode, permission.PermissionName);
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
    /// 转换为输入规则项
    /// </summary>
    private static IReadOnlyList<ConstraintRuleItemInputDto> ToInputItems(IReadOnlyList<SysConstraintRuleItem> items)
    {
        return items
            .Select(item => new ConstraintRuleItemInputDto
            {
                TargetType = item.TargetType,
                TargetId = item.TargetId,
                ConstraintGroup = item.ConstraintGroup,
                Remark = item.Remark
            })
            .ToArray();
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(ConstraintRuleCreateDto input, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.RuleCode);
        ValidateRuleCode(input.RuleCode);
        ValidateCommonInput(
            input.RuleName,
            input.ConstraintType,
            input.TargetType,
            input.Parameters,
            input.ViolationAction,
            input.Description,
            input.Priority,
            input.EffectiveTime,
            input.ExpirationTime,
            input.Remark,
            now);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(ConstraintRuleUpdateDto input, DateTimeOffset now)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "约束规则主键必须大于 0。");
        }

        ValidateCommonInput(
            input.RuleName,
            input.ConstraintType,
            input.TargetType,
            input.Parameters,
            input.ViolationAction,
            input.Description,
            input.Priority,
            input.EffectiveTime,
            input.ExpirationTime,
            input.Remark,
            now);
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonInput(
        string ruleName,
        ConstraintType constraintType,
        ConstraintTargetType targetType,
        string? parameters,
        ViolationAction violationAction,
        string? description,
        int priority,
        DateTimeOffset? effectiveTime,
        DateTimeOffset? expirationTime,
        string? remark,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        ValidateEnum(constraintType, nameof(constraintType));
        ValidateEnum(targetType, nameof(targetType));
        ValidateEnum(violationAction, nameof(violationAction));
        ValidateLength(ruleName, 200, nameof(ruleName), "规则名称不能超过 200 个字符。");
        ValidateOptionalLength(description, 1000, nameof(description), "规则描述不能超过 1000 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        _ = NormalizeParameters(parameters);

        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), "优先级不能小于 0。");
        }

        if (expirationTime.HasValue && expirationTime.Value <= now)
        {
            throw new InvalidOperationException("失效时间必须晚于当前时间。");
        }

        if (effectiveTime.HasValue && expirationTime.HasValue && effectiveTime.Value >= expirationTime.Value)
        {
            throw new InvalidOperationException("生效时间必须早于失效时间。");
        }
    }

    /// <summary>
    /// 校验规则编码
    /// </summary>
    private static void ValidateRuleCode(string ruleCode)
    {
        var normalizedRuleCode = ruleCode.Trim();
        ValidateLength(normalizedRuleCode, 100, nameof(ruleCode), "规则编码不能超过 100 个字符。");
        if (normalizedRuleCode.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("规则编码不能包含空白字符。");
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
    /// 规范化约束参数
    /// </summary>
    private static string? NormalizeParameters(string? parameters)
    {
        var normalized = NormalizeNullable(parameters);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("约束参数必须是合法 JSON。", exception);
        }

        return normalized;
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
