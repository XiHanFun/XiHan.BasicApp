#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsConfigApplicationMapper
// Guid:5e0b3a72-9c16-4f48-8d25-1a7c4b9f3e60
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 短信配置应用层映射器
/// </summary>
public static class SmsConfigApplicationMapper
{
    /// <summary>
    /// 映射短信配置创建命令
    /// </summary>
    public static SmsConfigCreateCommand ToCreateCommand(SmsConfigCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new SmsConfigCreateCommand(
            input.ConfigCode,
            input.ConfigName,
            input.Provider,
            input.AccessKeyId,
            input.AccessKeySecret,
            input.SdkAppId,
            input.SignName,
            input.Region,
            input.TemplateMap,
            input.IsDefault,
            input.IsEnabled,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射短信配置更新命令
    /// </summary>
    public static SmsConfigUpdateCommand ToUpdateCommand(SmsConfigUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new SmsConfigUpdateCommand(
            input.BasicId,
            input.ConfigName,
            input.Provider,
            input.AccessKeyId,
            input.AccessKeySecret,
            input.SdkAppId,
            input.SignName,
            input.Region,
            input.TemplateMap,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射短信配置状态命令
    /// </summary>
    public static SmsConfigStatusChangeCommand ToStatusCommand(SmsConfigStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new SmsConfigStatusChangeCommand(input.BasicId, input.IsEnabled);
    }

    /// <summary>
    /// 映射短信配置默认变更命令
    /// </summary>
    public static SmsConfigDefaultChangeCommand ToDefaultCommand(SmsConfigDefaultUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new SmsConfigDefaultChangeCommand(input.BasicId);
    }

    /// <summary>
    /// 映射短信配置列表项（AccessKeySecret 脱敏，仅标识是否已配置）
    /// </summary>
    /// <param name="config">短信配置</param>
    /// <returns>短信配置列表项 DTO</returns>
    public static SmsConfigListItemDto ToListItemDto(SysSmsConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new SmsConfigListItemDto
        {
            BasicId = config.BasicId,
            ConfigCode = config.ConfigCode,
            ConfigName = config.ConfigName,
            Provider = config.Provider,
            SignName = config.SignName,
            SdkAppId = config.SdkAppId,
            Region = config.Region,
            HasAccessKeySecret = !string.IsNullOrEmpty(config.AccessKeySecret),
            IsDefault = config.IsDefault,
            IsEnabled = config.IsEnabled,
            Sort = config.Sort,
            CreatedTime = config.CreatedTime,
            ModifiedTime = config.ModifiedTime
        };
    }

    /// <summary>
    /// 映射短信配置详情（AccessKeySecret 脱敏，仅标识是否已配置）
    /// </summary>
    /// <param name="config">短信配置</param>
    /// <returns>短信配置详情 DTO</returns>
    public static SmsConfigDetailDto ToDetailDto(SysSmsConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new SmsConfigDetailDto
        {
            BasicId = config.BasicId,
            ConfigCode = config.ConfigCode,
            ConfigName = config.ConfigName,
            Provider = config.Provider,
            AccessKeyId = config.AccessKeyId,
            HasAccessKeySecret = !string.IsNullOrEmpty(config.AccessKeySecret),
            SdkAppId = config.SdkAppId,
            SignName = config.SignName,
            Region = config.Region,
            TemplateMap = config.TemplateMap,
            IsDefault = config.IsDefault,
            IsEnabled = config.IsEnabled,
            Sort = config.Sort,
            Remark = config.Remark,
            CreatedTime = config.CreatedTime,
            CreatedId = config.CreatedId,
            CreatedBy = config.CreatedBy,
            ModifiedTime = config.ModifiedTime,
            ModifiedId = config.ModifiedId,
            ModifiedBy = config.ModifiedBy
        };
    }
}
