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

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
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
        return config?.ToDto();
    }

    /// <summary>
    /// 根据配置类型获取配置列表
    /// </summary>
    public async Task<List<ConfigDto>> GetByTypeAsync(ConfigType configType)
    {
        var configs = await _configRepository.GetByTypeAsync(configType);
        return configs.ToDto();
    }

    /// <summary>
    /// 根据租户ID获取配置列表
    /// </summary>
    public async Task<List<ConfigDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        var configs = await _configRepository.GetByTenantIdAsync(tenantId);
        return configs.ToDto();
    }

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    public async Task<bool> ExistsByKeyAsync(string configKey, XiHanBasicAppIdType? excludeId = null)
    {
        return await _configRepository.ExistsByKeyAsync(configKey, excludeId);
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<ConfigDto> MapToEntityDtoAsync(SysConfig entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 ConfigDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysConfig> MapToEntityAsync(ConfigDto dto)
    {
        var entity = new SysConfig
        {
            TenantId = dto.TenantId,
            ConfigKey = dto.ConfigKey,
            ConfigName = dto.ConfigName,
            ConfigValue = dto.ConfigValue,
            DefaultValue = dto.DefaultValue,
            ConfigType = dto.ConfigType,
            DataType = dto.DataType,
            ConfigDescription = dto.ConfigDescription,
            IsBuiltIn = dto.IsBuiltIn,
            IsEncrypted = dto.IsEncrypted,
            Status = dto.Status,
            Sort = dto.Sort,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 ConfigDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(ConfigDto dto, SysConfig entity)
    {
        entity.ConfigName = dto.ConfigName;
        entity.ConfigValue = dto.ConfigValue;
        entity.DefaultValue = dto.DefaultValue;
        entity.ConfigType = dto.ConfigType;
        entity.DataType = dto.DataType;
        entity.ConfigDescription = dto.ConfigDescription;
        entity.IsEncrypted = dto.IsEncrypted;
        entity.Status = dto.Status;
        entity.Sort = dto.Sort;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysConfig> MapToEntityAsync(CreateConfigDto createDto)
    {
        var entity = new SysConfig
        {
            TenantId = createDto.TenantId,
            ConfigKey = createDto.ConfigKey,
            ConfigName = createDto.ConfigName,
            ConfigValue = createDto.ConfigValue,
            DefaultValue = createDto.DefaultValue,
            ConfigType = createDto.ConfigType,
            DataType = createDto.DataType,
            ConfigDescription = createDto.ConfigDescription,
            IsEncrypted = createDto.IsEncrypted,
            Sort = createDto.Sort,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateConfigDto updateDto, SysConfig entity)
    {
        if (updateDto.ConfigName != null) entity.ConfigName = updateDto.ConfigName;
        if (updateDto.ConfigValue != null) entity.ConfigValue = updateDto.ConfigValue;
        if (updateDto.DefaultValue != null) entity.DefaultValue = updateDto.DefaultValue;
        if (updateDto.ConfigType.HasValue) entity.ConfigType = updateDto.ConfigType.Value;
        if (updateDto.DataType.HasValue) entity.DataType = updateDto.DataType.Value;
        if (updateDto.ConfigDescription != null) entity.ConfigDescription = updateDto.ConfigDescription;
        if (updateDto.IsEncrypted.HasValue) entity.IsEncrypted = updateDto.IsEncrypted.Value;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Sort.HasValue) entity.Sort = updateDto.Sort.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
