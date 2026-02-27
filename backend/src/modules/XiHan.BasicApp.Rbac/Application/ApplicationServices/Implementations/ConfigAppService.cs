#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigAppService
// Guid:df353c36-874e-4f45-b9e6-815ad6c9cb00
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
