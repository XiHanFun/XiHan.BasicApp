#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfigService
// Guid:p1q2r3s4-t5u6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Configs;
using XiHan.BasicApp.Rbac.Services.Configs.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Configs;

/// <summary>
/// 系统配置服务实现
/// </summary>
public class SysConfigService : CrudApplicationServiceBase<SysConfig, ConfigDto, XiHanBasicAppIdType, CreateConfigDto, UpdateConfigDto>, ISysConfigService
{
    private readonly ISysConfigRepository _configRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysConfigService(ISysConfigRepository configRepository) : base(configRepository)
    {
        _configRepository = configRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    public async Task<ConfigDto?> GetByKeyAsync(string configKey)
    {
        var config = await _configRepository.GetByKeyAsync(configKey);
        return config.Adapt<ConfigDto>();
    }

    /// <summary>
    /// 根据配置类型获取配置列表
    /// </summary>
    public async Task<List<ConfigDto>> GetByTypeAsync(ConfigType configType)
    {
        var configs = await _configRepository.GetByTypeAsync(configType);
        return configs.Adapt<List<ConfigDto>>();
    }

    /// <summary>
    /// 根据租户ID获取配置列表
    /// </summary>
    public async Task<List<ConfigDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        var configs = await _configRepository.GetByTenantIdAsync(tenantId);
        return configs.Adapt<List<ConfigDto>>();
    }

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    public async Task<bool> ExistsByKeyAsync(string configKey, XiHanBasicAppIdType? excludeId = null)
    {
        return await _configRepository.ExistsByKeyAsync(configKey, excludeId);
    }

    #endregion 业务特定方法
}
