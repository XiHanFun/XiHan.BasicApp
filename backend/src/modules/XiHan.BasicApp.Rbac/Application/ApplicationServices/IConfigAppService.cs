using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 配置应用服务
/// </summary>
public interface IConfigAppService : IApplicationService
{
    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    /// <param name="configKey"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<ConfigDto?> GetConfigByKeyAsync(string configKey, long? tenantId = null);
}
