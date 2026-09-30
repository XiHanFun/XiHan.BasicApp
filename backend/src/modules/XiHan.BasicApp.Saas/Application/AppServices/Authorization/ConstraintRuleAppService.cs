// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using XiHan.BasicApp.Saas.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 约束规则命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "约束规则")]
public sealed class ConstraintRuleAppService
    : SaasApplicationService, IConstraintRuleAppService
{
    private readonly IConstraintRuleDomainService _constraintRuleDomainService;
    private readonly IConstraintRuleQueryService _constraintRuleQueryService;

    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConstraintRuleAppService(
        IConstraintRuleDomainService constraintRuleDomainService,
        IConstraintRuleQueryService constraintRuleQueryService,
        IFieldSecurityService fieldSecurity)
    {
        _constraintRuleDomainService = constraintRuleDomainService;
        _constraintRuleQueryService = constraintRuleQueryService;
        _fieldSecurity = fieldSecurity;
    }

    /// <summary>
    /// 创建约束规则
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Create)]
    public async Task<ConstraintRuleDetailDto> CreateConstraintRuleAsync(ConstraintRuleCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能填写
        await _fieldSecurity.EnsureCreatableAsync(typeof(SysConstraintRule), input, cancellationToken);

        var result = await _constraintRuleDomainService.CreateConstraintRuleAsync(ConstraintRuleApplicationMapper.ToCreateCommand(input), cancellationToken);
        return await GetDetailOrThrowAsync(result.RuleId, cancellationToken);
    }

    /// <summary>
    /// 删除约束规则
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Delete)]
    public async Task DeleteConstraintRuleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _constraintRuleDomainService.DeleteConstraintRuleAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新约束规则
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Update)]
    public async Task<ConstraintRuleDetailDto> UpdateConstraintRuleAsync(ConstraintRuleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysConstraintRule), input.BasicId, input, cancellationToken);

        var result = await _constraintRuleDomainService.UpdateConstraintRuleAsync(ConstraintRuleApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return await GetDetailOrThrowAsync(result.RuleId, cancellationToken);
    }

    /// <summary>
    /// 更新约束规则状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.ConstraintRule.Status)]
    public async Task<ConstraintRuleDetailDto> UpdateConstraintRuleStatusAsync(ConstraintRuleStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysConstraintRule), input.BasicId, input, cancellationToken);

        var result = await _constraintRuleDomainService.UpdateConstraintRuleStatusAsync(ConstraintRuleApplicationMapper.ToStatusCommand(input), cancellationToken);
        return await GetDetailOrThrowAsync(result.RuleId, cancellationToken);
    }

    private async Task<ConstraintRuleDetailDto> GetDetailOrThrowAsync(long ruleId, CancellationToken cancellationToken)
    {
        return await _constraintRuleQueryService.GetConstraintRuleDetailAsync(ruleId, cancellationToken)
            ?? throw new InvalidOperationException("约束规则不存在。");
    }
}
