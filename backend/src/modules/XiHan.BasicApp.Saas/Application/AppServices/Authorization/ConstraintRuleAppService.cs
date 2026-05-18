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

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

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

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConstraintRuleAppService(
        IConstraintRuleDomainService constraintRuleDomainService,
        IConstraintRuleQueryService constraintRuleQueryService)
    {
        _constraintRuleDomainService = constraintRuleDomainService;
        _constraintRuleQueryService = constraintRuleQueryService;
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

        var result = await _constraintRuleDomainService.UpdateConstraintRuleStatusAsync(ConstraintRuleApplicationMapper.ToStatusCommand(input), cancellationToken);
        return await GetDetailOrThrowAsync(result.RuleId, cancellationToken);
    }

    private async Task<ConstraintRuleDetailDto> GetDetailOrThrowAsync(long ruleId, CancellationToken cancellationToken)
    {
        return await _constraintRuleQueryService.GetConstraintRuleDetailAsync(ruleId, cancellationToken)
            ?? throw new InvalidOperationException("约束规则不存在。");
    }
}
