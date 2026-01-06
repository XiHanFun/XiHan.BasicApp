#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConstraintRuleRepository
// Guid:0a2b3c4d-5e6f-7890-abcd-ef1234567809
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.ConstraintRules;

/// <summary>
/// 系统约束规则仓储实现
/// </summary>
public class SysConstraintRuleRepository : SqlSugarRepositoryBase<SysConstraintRule, long>, ISysConstraintRuleRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysConstraintRuleRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据规则名称获取规则
    /// </summary>
    public async Task<SysConstraintRule?> GetByRuleNameAsync(string ruleName)
    {
        return await GetFirstAsync(r => r.RuleName == ruleName);
    }

    /// <summary>
    /// 根据约束类型获取规则列表
    /// </summary>
    public async Task<List<SysConstraintRule>> GetByConstraintTypeAsync(ConstraintType constraintType)
    {
        return await GetListAsync(r => r.ConstraintType == constraintType);
    }

    /// <summary>
    /// 获取已启用的规则列表
    /// </summary>
    public async Task<List<SysConstraintRule>> GetEnabledRulesAsync()
    {
        return await GetListAsync(r => r.IsEnabled == true);
    }

    /// <summary>
    /// 根据目标类型获取规则列表
    /// </summary>
    public async Task<List<SysConstraintRule>> GetByTargetTypeAsync(string targetType)
    {
        return await GetListAsync(r => r.TargetType == targetType);
    }

    /// <summary>
    /// 检查规则是否存在
    /// </summary>
    public async Task<bool> ExistsByRuleNameAsync(string ruleName, long? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysConstraintRule>().Where(r => r.RuleName == ruleName);
        if (excludeId.HasValue)
        {
            query = query.Where(r => r.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }
}
