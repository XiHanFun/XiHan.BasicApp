#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysConstraintRuleRepository
// Guid:9a2b3c4d-5e6f-7890-abcd-ef1234567808
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.ConstraintRules;

/// <summary>
/// 系统约束规则仓储接口
/// </summary>
public interface ISysConstraintRuleRepository : IRepositoryBase<SysConstraintRule, long>
{
    /// <summary>
    /// 根据规则名称获取规则
    /// </summary>
    /// <param name="ruleName">规则名称</param>
    /// <returns></returns>
    Task<SysConstraintRule?> GetByRuleNameAsync(string ruleName);

    /// <summary>
    /// 根据约束类型获取规则列表
    /// </summary>
    /// <param name="constraintType">约束类型</param>
    /// <returns></returns>
    Task<List<SysConstraintRule>> GetByConstraintTypeAsync(ConstraintType constraintType);

    /// <summary>
    /// 获取已启用的规则列表
    /// </summary>
    /// <returns></returns>
    Task<List<SysConstraintRule>> GetEnabledRulesAsync();

    /// <summary>
    /// 根据目标类型获取规则列表
    /// </summary>
    /// <param name="targetType">目标类型</param>
    /// <returns></returns>
    Task<List<SysConstraintRule>> GetByTargetTypeAsync(string targetType);

    /// <summary>
    /// 检查规则是否存在
    /// </summary>
    /// <param name="ruleName">规则名称</param>
    /// <param name="excludeId">排除的ID</param>
    /// <returns></returns>
    Task<bool> ExistsByRuleNameAsync(string ruleName, long? excludeId = null);
}
