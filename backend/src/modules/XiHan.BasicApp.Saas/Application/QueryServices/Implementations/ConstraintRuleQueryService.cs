using Mapster;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 约束规则查询服务
/// </summary>
public class ConstraintRuleQueryService : IConstraintRuleQueryService, ITransientDependency
{
    private readonly IConstraintRuleRepository _constraintRuleRepository;

    public ConstraintRuleQueryService(IConstraintRuleRepository constraintRuleRepository)
    {
        _constraintRuleRepository = constraintRuleRepository;
    }

    [Cacheable(Key = QueryCacheKeys.ConstraintRuleById, ExpireSeconds = 300)]
    public async Task<ConstraintRuleDto?> GetByIdAsync(long id)
    {
        var entity = await _constraintRuleRepository.GetByIdAsync(id);
        return entity?.Adapt<ConstraintRuleDto>();
    }

    public async Task<ConstraintRuleDto?> GetByCodeAsync(string ruleCode, long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleCode);
        var entity = await _constraintRuleRepository.GetByRuleCodeAsync(ruleCode, tenantId);
        return entity?.Adapt<ConstraintRuleDto>();
    }
}
