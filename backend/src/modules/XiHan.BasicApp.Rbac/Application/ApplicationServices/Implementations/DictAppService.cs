using Mapster;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 字典应用服务
/// </summary>
public class DictAppService : ApplicationServiceBase, IDictAppService
{
    private readonly IDictRepository _dictRepository;
    private readonly IDictItemRepository _dictItemRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dictRepository"></param>
    /// <param name="dictItemRepository"></param>
    public DictAppService(
        IDictRepository dictRepository,
        IDictItemRepository dictItemRepository)
    {
        _dictRepository = dictRepository;
        _dictItemRepository = dictItemRepository;
    }

    /// <summary>
    /// 根据字典编码获取字典信息
    /// </summary>
    /// <param name="dictCode"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<DictDto?> GetDictByCodeAsync(string dictCode, long? tenantId = null)
    {
        var entity = await _dictRepository.GetByDictCodeAsync(dictCode, tenantId);
        return entity?.Adapt<DictDto>();
    }

    /// <summary>
    /// 根据字典ID获取字典项
    /// </summary>
    /// <param name="dictId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<DictItemDto>> GetDictItemsAsync(long dictId, long? tenantId = null)
    {
        var entities = await _dictItemRepository.GetByDictIdAsync(dictId, tenantId);
        return entities.Select(static entity => entity.Adapt<DictItemDto>()).ToArray();
    }
}
