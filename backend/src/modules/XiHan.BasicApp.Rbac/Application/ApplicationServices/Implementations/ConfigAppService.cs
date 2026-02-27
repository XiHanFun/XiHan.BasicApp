using Mapster;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 配置应用服务
/// </summary>
public class ConfigAppService : ApplicationServiceBase, IConfigAppService
{
    private readonly IConfigRepository _configRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="configRepository"></param>
    public ConfigAppService(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    /// <summary>
    /// 根据配置键获取配置信息
    /// </summary>
    /// <param name="configKey"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<ConfigDto?> GetConfigByKeyAsync(string configKey, long? tenantId = null)
    {
        var entity = await _configRepository.GetByConfigKeyAsync(configKey, tenantId);
        return entity?.Adapt<ConfigDto>();
    }
}
