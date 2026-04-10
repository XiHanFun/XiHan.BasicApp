#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConstraintRuleRepository
// Guid:9d2b8d0f-7548-4b1f-8db6-98e6612f1972
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 约束规则仓储接口
/// </summary>
public interface IConstraintRuleRepository : IRepositoryBase<SysConstraintRule, long>
{
    /// <summary>
    /// 根据规则编码获取规则
    /// </summary>
    /// <param name="ruleCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SysConstraintRule?> GetByRuleCodeAsync(string ruleCode, long? tenantId = null, CancellationToken cancellationToken = default);
}
