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
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 配置应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class ConfigAppService
    : CrudApplicationServiceBase<SysConfig, ConfigDto, long, ConfigCreateDto, ConfigUpdateDto, BasicAppPRDto>,
        IConfigAppService
{
    private readonly IConfigQueryService _queryService;
    private readonly IConfigDomainService _domainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigAppService(
        IConfigRepository configRepository,
        IConfigQueryService queryService,
        IConfigDomainService domainService)
        : base(configRepository)
    {
        _queryService = queryService;
        _domainService = domainService;
    }

    /// <summary>
    /// 根据配置键获取配置（委托 QueryService，走缓存）
    /// </summary>
    public async Task<ConfigDto?> GetConfigByKeyAsync(string configKey, long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configKey);
        return await _queryService.GetByKeyAsync(configKey, tenantId);
    }

    /// <summary>
    /// 创建配置（委托 DomainService）
    /// </summary>
    public override async Task<ConfigDto> CreateAsync(ConfigCreateDto input)
    {
        input.ValidateAnnotations();
        var entity = await MapDtoToEntityAsync(input);
        var created = await _domainService.CreateAsync(entity);
        return created.Adapt<ConfigDto>()!;
    }

    /// <summary>
    /// 更新配置（委托 DomainService）
    /// </summary>
    public override async Task<ConfigDto> UpdateAsync(ConfigUpdateDto input)
    {
        input.ValidateAnnotations();
        var entity = await Repository.GetByIdAsync(input.BasicId)
                     ?? throw new KeyNotFoundException($"未找到配置: {input.BasicId}");
        await MapDtoToEntityAsync(input, entity);
        var updated = await _domainService.UpdateAsync(entity);
        return updated.Adapt<ConfigDto>()!;
    }

    /// <summary>
    /// 删除配置（委托 DomainService）
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        return await _domainService.DeleteAsync(id);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
}
