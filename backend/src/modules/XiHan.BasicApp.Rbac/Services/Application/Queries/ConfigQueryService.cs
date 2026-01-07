#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigQueryService
// Guid:c6d7e8f9-a0b1-4c5d-2e3f-5a6b7c8d9e0f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 配置查询服务（处理配置的读操作 - CQRS）
/// </summary>
public class ConfigQueryService : ApplicationServiceBase
{
    private readonly IConfigRepository _configRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigQueryService(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    /// <summary>
    /// 根据ID获取配置
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <returns>配置DTO</returns>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var config = await _configRepository.GetByIdAsync(id);
        return config?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <returns>配置DTO</returns>
    public async Task<RbacDtoBase?> GetByConfigKeyAsync(string configKey)
    {
        var config = await _configRepository.GetByConfigKeyAsync(configKey);
        return config?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据配置组获取配置列表
    /// </summary>
    /// <param name="configGroup">配置组</param>
    /// <returns>配置DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByConfigGroupAsync(string configGroup)
    {
        var configs = await _configRepository.GetByConfigGroupAsync(configGroup);
        return configs.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取所有系统配置
    /// </summary>
    /// <returns>配置字典（Key-Value）</returns>
    public async Task<Dictionary<string, string>> GetAllConfigsAsync()
    {
        return await _configRepository.GetAllConfigsAsync();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="input">分页查询参数</param>
    /// <returns>分页响应</returns>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _configRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
