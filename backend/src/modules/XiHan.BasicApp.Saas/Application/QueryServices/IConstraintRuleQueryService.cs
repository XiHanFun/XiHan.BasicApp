using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 约束规则查询服务接口
/// </summary>
public interface IConstraintRuleQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取规则。
    /// </summary>
    Task<ConstraintRuleDto?> GetByIdAsync(long id);

    /// <summary>
    /// 根据编码获取规则。
    /// </summary>
    Task<ConstraintRuleDto?> GetByCodeAsync(string ruleCode, long? tenantId = null);
}
