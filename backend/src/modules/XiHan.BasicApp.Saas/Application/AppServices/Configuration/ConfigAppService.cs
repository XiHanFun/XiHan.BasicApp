#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigAppService
// Guid:f6e85c17-b95e-4c4b-b7aa-e5561cb8ebd1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统配置命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统配置")]
public sealed class ConfigAppService
    : SaasApplicationService, IConfigAppService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigAppService(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    private readonly IConfigRepository _configRepository;

    /// <summary>
    /// 创建系统配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Config.Create)]
    public async Task<ConfigDetailDto> CreateConfigAsync(ConfigCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);
        var configKey = Required(input.ConfigKey, 100, nameof(input.ConfigKey), "配置键不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(configKey, "配置键不能包含空白字符。");
        if (await _configRepository.AnyAsync(config => config.ConfigKey == configKey, cancellationToken))
        {
            throw new InvalidOperationException("配置键已存在。");
        }

        var config = new SysConfig
        {
            IsGlobal = input.IsGlobal,
            ConfigName = Required(input.ConfigName, 100, nameof(input.ConfigName), "配置名称不能超过 100 个字符。"),
            ConfigGroup = Optional(input.ConfigGroup, 100, nameof(input.ConfigGroup), "配置分组不能超过 100 个字符。"),
            ConfigKey = configKey,
            ConfigValue = NormalizeNullable(input.ConfigValue),
            DefaultValue = NormalizeNullable(input.DefaultValue),
            ConfigType = input.ConfigType,
            DataType = input.DataType,
            ConfigDescription = Optional(input.ConfigDescription, 500, nameof(input.ConfigDescription), "配置描述不能超过 500 个字符。"),
            IsBuiltIn = false,
            IsEncrypted = input.IsEncrypted,
            Status = input.Status,
            Sort = input.Sort,
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };

        var savedConfig = await _configRepository.AddAsync(config, cancellationToken);
        return ConfigApplicationMapper.ToDetailDto(savedConfig);
    }

    /// <summary>
    /// 更新系统配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Config.Update)]
    public async Task<ConfigDetailDto> UpdateConfigAsync(ConfigUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);
        var config = await GetConfigOrThrowAsync(input.BasicId, cancellationToken);
        config.ConfigName = Required(input.ConfigName, 100, nameof(input.ConfigName), "配置名称不能超过 100 个字符。");
        config.ConfigGroup = Optional(input.ConfigGroup, 100, nameof(input.ConfigGroup), "配置分组不能超过 100 个字符。");
        config.ConfigValue = NormalizeNullable(input.ConfigValue);
        config.DefaultValue = NormalizeNullable(input.DefaultValue);
        config.ConfigType = input.ConfigType;
        config.DataType = input.DataType;
        config.ConfigDescription = Optional(input.ConfigDescription, 500, nameof(input.ConfigDescription), "配置描述不能超过 500 个字符。");
        config.IsEncrypted = input.IsEncrypted;
        config.Sort = input.Sort;
        config.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var savedConfig = await _configRepository.UpdateAsync(config, cancellationToken);
        return ConfigApplicationMapper.ToDetailDto(savedConfig);
    }

    /// <summary>
    /// 更新系统配置状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Config.Status)]
    public async Task<ConfigDetailDto> UpdateConfigStatusAsync(ConfigStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统配置主键必须大于 0。");
        EnsureEnum(input.Status, nameof(input.Status));

        var config = await GetConfigOrThrowAsync(input.BasicId, cancellationToken);
        config.Status = input.Status;
        config.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? config.Remark;

        var savedConfig = await _configRepository.UpdateAsync(config, cancellationToken);
        return ConfigApplicationMapper.ToDetailDto(savedConfig);
    }

    /// <summary>
    /// 删除系统配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Config.Delete)]
    public async Task DeleteConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = await GetConfigOrThrowAsync(id, cancellationToken);
        if (config.IsBuiltIn)
        {
            throw new InvalidOperationException("内置系统配置不能删除。");
        }

        if (!await _configRepository.DeleteAsync(config, cancellationToken))
        {
            throw new InvalidOperationException("系统配置删除失败。");
        }
    }

    private async Task<SysConfig> GetConfigOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统配置主键必须大于 0。");
        return await _configRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统配置不存在。");
    }

    private static void ValidateCreateInput(ConfigCreateDto input)
    {
        EnsureEnum(input.ConfigType, nameof(input.ConfigType));
        EnsureEnum(input.DataType, nameof(input.DataType));
        EnsureEnum(input.Status, nameof(input.Status));
    }

    private static void ValidateUpdateInput(ConfigUpdateDto input)
    {
        EnsureId(input.BasicId, "系统配置主键必须大于 0。");
        EnsureEnum(input.ConfigType, nameof(input.ConfigType));
        EnsureEnum(input.DataType, nameof(input.DataType));
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }
}
