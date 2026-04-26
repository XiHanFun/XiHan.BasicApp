#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConstraintRuleRepository
// Guid:fde80a74-2bb7-4823-81d9-c30d9f64fbde
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 约束规则仓储接口
/// </summary>
public interface IConstraintRuleRepository : ISaasRepository<SysConstraintRule>
{
    /// <summary>
    /// 获取当前生效的约束规则
    /// </summary>
    Task<IReadOnlyList<SysConstraintRule>> GetActiveRulesAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
