#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigApplicationMapper
// Guid:7b453f74-c2d0-4e86-8d1c-07e2957a8b21
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统配置应用层映射器
/// </summary>
public static class ConfigApplicationMapper
{
    /// <summary>
    /// 映射系统配置创建命令
    /// </summary>
    public static ConfigCreateCommand ToCreateCommand(ConfigCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ConfigCreateCommand(
            input.IsGlobal,
            input.ConfigName,
            input.ConfigGroup,
            input.ConfigKey,
            input.ConfigValue,
            input.DefaultValue,
            input.ConfigType,
            input.DataType,
            input.ConfigDescription,
            input.IsEncrypted,
            input.Status,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射系统配置更新命令
    /// </summary>
    public static ConfigUpdateCommand ToUpdateCommand(ConfigUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ConfigUpdateCommand(
            input.BasicId,
            input.ConfigName,
            input.ConfigGroup,
            input.ConfigValue,
            input.DefaultValue,
            input.ConfigType,
            input.DataType,
            input.ConfigDescription,
            input.IsEncrypted,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射系统配置状态命令
    /// </summary>
    public static ConfigStatusChangeCommand ToStatusCommand(ConfigStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ConfigStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射系统配置列表项
    /// </summary>
    /// <param name="config">系统配置实体</param>
    /// <returns>系统配置列表项 DTO</returns>
    public static ConfigListItemDto ToListItemDto(SysConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new ConfigListItemDto
        {
            BasicId = config.BasicId,
            IsGlobal = config.IsGlobal,
            ConfigName = config.ConfigName,
            ConfigGroup = config.ConfigGroup,
            ConfigKey = config.ConfigKey,
            ConfigType = config.ConfigType,
            DataType = config.DataType,
            ConfigDescription = config.ConfigDescription,
            IsBuiltIn = config.IsBuiltIn,
            IsEncrypted = config.IsEncrypted,
            Status = config.Status,
            Sort = config.Sort,
            CreatedTime = config.CreatedTime,
            ModifiedTime = config.ModifiedTime,
            HasCurrentValue = !string.IsNullOrWhiteSpace(config.ConfigValue),
            HasFallbackValue = !string.IsNullOrWhiteSpace(config.DefaultValue),
            HasNote = !string.IsNullOrWhiteSpace(config.Remark)
        };
    }

    /// <summary>
    /// 映射系统配置详情
    /// </summary>
    /// <param name="config">系统配置实体</param>
    /// <returns>系统配置详情 DTO</returns>
    public static ConfigDetailDto ToDetailDto(SysConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var item = ToListItemDto(config);
        return new ConfigDetailDto
        {
            BasicId = item.BasicId,
            IsGlobal = item.IsGlobal,
            ConfigName = item.ConfigName,
            ConfigGroup = item.ConfigGroup,
            ConfigKey = item.ConfigKey,
            ConfigType = item.ConfigType,
            DataType = item.DataType,
            ConfigDescription = item.ConfigDescription,
            IsBuiltIn = item.IsBuiltIn,
            IsEncrypted = item.IsEncrypted,
            Status = item.Status,
            Sort = item.Sort,
            CreatedTime = item.CreatedTime,
            ModifiedTime = item.ModifiedTime,
            HasCurrentValue = item.HasCurrentValue,
            HasFallbackValue = item.HasFallbackValue,
            HasNote = item.HasNote,
            // 加密项不回传明文，前端以「已加密」提示替代
            ConfigValue = config.IsEncrypted ? null : config.ConfigValue,
            DefaultValue = config.IsEncrypted ? null : config.DefaultValue,
            Remark = config.Remark,
            CreatedId = config.CreatedId,
            CreatedBy = config.CreatedBy,
            ModifiedId = config.ModifiedId,
            ModifiedBy = config.ModifiedBy
        };
    }
}
