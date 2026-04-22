#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleAppService
// Guid:86e4f9f8-b1b1-4357-a6f6-92d77f5a87f3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 约束规则应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
[PermissionAuthorize("constraint_rule:read")]
public class ConstraintRuleAppService
    : CrudApplicationServiceBase<SysConstraintRule, ConstraintRuleDto, long, ConstraintRuleCreateDto, ConstraintRuleUpdateDto, BasicAppPRDto>,
        IConstraintRuleAppService
{
    private readonly IConstraintRuleRepository _constraintRuleRepository;
    private readonly IConstraintRuleQueryService _queryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="constraintRuleRepository"></param>
    public ConstraintRuleAppService(
        IConstraintRuleRepository constraintRuleRepository,
        IConstraintRuleQueryService queryService)
        : base(constraintRuleRepository)
    {
        _constraintRuleRepository = constraintRuleRepository;
        _queryService = queryService;
    }

    [PermissionAuthorize("constraint_rule:read")]
    public override async Task<ConstraintRuleDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据规则编码获取规则
    /// </summary>
    /// <param name="ruleCode"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("constraint_rule:read")]
    public async Task<ConstraintRuleDto?> GetRuleByCodeAsync(string ruleCode, long? tenantId = null)
    {
        return await _queryService.GetByCodeAsync(ruleCode, tenantId);
    }

    /// <summary>
    /// 创建约束规则
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("constraint_rule:create")]
    public override async Task<ConstraintRuleDto> CreateAsync(ConstraintRuleCreateDto input)
    {
        input.ValidateAnnotations();

        await EnsureRuleCodeUniqueAsync(input.RuleCode, input.TenantId, null);

        var entity = await MapDtoToEntityAsync(input);
        var created = await _constraintRuleRepository.AddAsync(entity);
        return created.Adapt<ConstraintRuleDto>()!;
    }

    /// <summary>
    /// 更新约束规则
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("constraint_rule:update")]
    public override async Task<ConstraintRuleDto> UpdateAsync(ConstraintRuleUpdateDto input)
    {
        input.ValidateAnnotations();

        var entity = await _constraintRuleRepository.GetByIdAsync(input.BasicId)
                     ?? throw new KeyNotFoundException($"未找到约束规则: {input.BasicId}");

        await EnsureRuleCodeUniqueAsync(input.RuleCode, entity.TenantId, input.BasicId);

        await MapDtoToEntityAsync(input, entity);
        var updated = await _constraintRuleRepository.UpdateAsync(entity);
        return updated.Adapt<ConstraintRuleDto>()!;
    }

    /// <summary>
    /// 删除约束规则
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [PermissionAuthorize("constraint_rule:delete")]
    public override async Task<bool> DeleteAsync(long id)
    {
        var entity = await _constraintRuleRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return false;
        }

        return await _constraintRuleRepository.DeleteAsync(entity);
    }

    /// <inheritdoc />
    protected override Task<SysConstraintRule> MapDtoToEntityAsync(ConstraintRuleCreateDto createDto)
    {
        var entity = new SysConstraintRule
        {
            TenantId = createDto.TenantId ?? 0,
            RuleCode = createDto.RuleCode.Trim(),
            RuleName = createDto.RuleName.Trim(),
            ConstraintType = createDto.ConstraintType,
            TargetType = createDto.TargetType,
            Parameters = string.IsNullOrWhiteSpace(createDto.Parameters) ? "{}" : createDto.Parameters.Trim(),
            IsEnabled = createDto.IsEnabled,
            ViolationAction = createDto.ViolationAction,
            Description = createDto.Description?.Trim(),
            Priority = createDto.Priority,
            EffectiveFrom = createDto.EffectiveFrom,
            EffectiveTo = createDto.EffectiveTo,
            Remark = createDto.Remark?.Trim()
        };

        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    protected override Task MapDtoToEntityAsync(ConstraintRuleUpdateDto updateDto, SysConstraintRule entity)
    {
        entity.RuleCode = updateDto.RuleCode.Trim();
        entity.RuleName = updateDto.RuleName.Trim();
        entity.ConstraintType = updateDto.ConstraintType;
        entity.TargetType = updateDto.TargetType;
        entity.Parameters = string.IsNullOrWhiteSpace(updateDto.Parameters) ? "{}" : updateDto.Parameters.Trim();
        entity.IsEnabled = updateDto.IsEnabled;
        entity.ViolationAction = updateDto.ViolationAction;
        entity.Description = updateDto.Description?.Trim();
        entity.Priority = updateDto.Priority;
        entity.EffectiveFrom = updateDto.EffectiveFrom;
        entity.EffectiveTo = updateDto.EffectiveTo;
        entity.Remark = updateDto.Remark?.Trim();

        return Task.CompletedTask;
    }

    private async Task EnsureRuleCodeUniqueAsync(string ruleCode, long? tenantId, long? excludeId)
    {
        var existing = await _constraintRuleRepository.GetByRuleCodeAsync(ruleCode.Trim(), tenantId);
        if (existing is not null && (!excludeId.HasValue || existing.BasicId != excludeId.Value))
        {
            throw new BusinessException(message: $"规则编码 '{ruleCode}' 已存在");
        }
    }
}
