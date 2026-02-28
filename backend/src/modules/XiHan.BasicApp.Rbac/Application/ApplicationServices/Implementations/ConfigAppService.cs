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
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 配置应用服务
/// </summary>
public class ConfigAppService
    : CrudApplicationServiceBase<SysConfig, ConfigDto, long, ConfigCreateDto, ConfigUpdateDto, BasicAppPRDto>,
        IConfigAppService
{
    private readonly IConfigRepository _configRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="configRepository"></param>
    public ConfigAppService(IConfigRepository configRepository)
        : base(configRepository)
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

    /// <summary>
    /// 创建配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<ConfigDto> CreateAsync(ConfigCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedKey = input.ConfigKey.Trim();
        await EnsureConfigKeyUniqueAsync(normalizedKey, null, input.TenantId);

        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<ConfigDto> UpdateAsync(long id, ConfigUpdateDto input)
    {
        input.ValidateAnnotations();

        var config = await _configRepository.GetByIdAsync(id)
                     ?? throw new KeyNotFoundException($"未找到配置: {id}");

        var normalizedKey = input.ConfigKey.Trim();
        await EnsureConfigKeyUniqueAsync(normalizedKey, id, config.TenantId);

        await MapDtoToEntityAsync(input, config);
        var updated = await _configRepository.UpdateAsync(config);
        return updated.Adapt<ConfigDto>();
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    /// <param name="createDto"></param>
    /// <returns></returns>
    protected override Task<SysConfig> MapDtoToEntityAsync(ConfigCreateDto createDto)
    {
        var entity = new SysConfig
        {
            TenantId = createDto.TenantId,
            ConfigName = createDto.ConfigName.Trim(),
            ConfigKey = createDto.ConfigKey.Trim(),
            ConfigValue = createDto.ConfigValue,
            ConfigType = createDto.ConfigType,
            DataType = createDto.DataType,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    /// <param name="updateDto"></param>
    /// <param name="entity"></param>
    protected override Task MapDtoToEntityAsync(ConfigUpdateDto updateDto, SysConfig entity)
    {
        entity.ConfigName = updateDto.ConfigName.Trim();
        entity.ConfigKey = updateDto.ConfigKey.Trim();
        entity.ConfigValue = updateDto.ConfigValue;
        entity.ConfigType = updateDto.ConfigType;
        entity.DataType = updateDto.DataType;
        entity.Status = updateDto.Status;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 校验配置键唯一性
    /// </summary>
    /// <param name="configKey"></param>
    /// <param name="excludeConfigId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsureConfigKeyUniqueAsync(string configKey, long? excludeConfigId, long? tenantId)
    {
        var existing = await _configRepository.GetByConfigKeyAsync(configKey, tenantId);
        if (existing is not null && (!excludeConfigId.HasValue || existing.BasicId != excludeConfigId.Value))
        {
            throw new InvalidOperationException($"配置键 '{configKey}' 已存在");
        }
    }
}
