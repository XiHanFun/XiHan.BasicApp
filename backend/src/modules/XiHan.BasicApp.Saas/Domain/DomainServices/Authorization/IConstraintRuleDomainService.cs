#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConstraintRuleDomainService
// Guid:7b83ded5-2317-40a4-aac8-bd04221e07f7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 约束规则领域服务
/// </summary>
public interface IConstraintRuleDomainService
{
    /// <summary>
    /// 创建约束规则
    /// </summary>
    Task<ConstraintRuleCommandResult> CreateConstraintRuleAsync(ConstraintRuleCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除约束规则
    /// </summary>
    Task DeleteConstraintRuleAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新约束规则
    /// </summary>
    Task<ConstraintRuleCommandResult> UpdateConstraintRuleAsync(ConstraintRuleUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新约束规则状态
    /// </summary>
    Task<ConstraintRuleCommandResult> UpdateConstraintRuleStatusAsync(ConstraintRuleStatusCommand command, CancellationToken cancellationToken = default);
}
