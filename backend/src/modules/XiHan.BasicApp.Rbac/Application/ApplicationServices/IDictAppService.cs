using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 字典应用服务
/// </summary>
public interface IDictAppService : IApplicationService
{
    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    /// <param name="dictCode"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<DictDto?> GetDictByCodeAsync(string dictCode, long? tenantId = null);

    /// <summary>
    /// 根据字典ID获取字典项
    /// </summary>
    /// <param name="dictId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<DictItemDto>> GetDictItemsAsync(long dictId, long? tenantId = null);
}
