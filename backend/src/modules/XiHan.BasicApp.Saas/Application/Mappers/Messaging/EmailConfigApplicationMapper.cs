#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailConfigApplicationMapper
// Guid:4d9a2f61-8b05-4e37-9c14-0f6b3a8e2d59
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
/// 邮件配置应用层映射器
/// </summary>
public static class EmailConfigApplicationMapper
{
    /// <summary>
    /// 映射邮件配置创建命令
    /// </summary>
    public static EmailConfigCreateCommand ToCreateCommand(EmailConfigCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new EmailConfigCreateCommand(
            input.ConfigCode,
            input.ConfigName,
            input.SmtpHost,
            input.SmtpPort,
            input.UseSsl,
            input.AcceptInvalidCertificate,
            input.FromEmail,
            input.FromName,
            input.UserName,
            input.Password,
            input.IsBodyHtml,
            input.IsDefault,
            input.IsEnabled,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射邮件配置更新命令
    /// </summary>
    public static EmailConfigUpdateCommand ToUpdateCommand(EmailConfigUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new EmailConfigUpdateCommand(
            input.BasicId,
            input.ConfigName,
            input.SmtpHost,
            input.SmtpPort,
            input.UseSsl,
            input.AcceptInvalidCertificate,
            input.FromEmail,
            input.FromName,
            input.UserName,
            input.Password,
            input.IsBodyHtml,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射邮件配置状态命令
    /// </summary>
    public static EmailConfigStatusChangeCommand ToStatusCommand(EmailConfigStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new EmailConfigStatusChangeCommand(input.BasicId, input.IsEnabled);
    }

    /// <summary>
    /// 映射邮件配置默认变更命令
    /// </summary>
    public static EmailConfigDefaultChangeCommand ToDefaultCommand(EmailConfigDefaultUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new EmailConfigDefaultChangeCommand(input.BasicId);
    }

    /// <summary>
    /// 映射邮件配置列表项（Password 脱敏，仅标识是否已配置）
    /// </summary>
    /// <param name="config">邮件配置</param>
    /// <returns>邮件配置列表项 DTO</returns>
    public static EmailConfigListItemDto ToListItemDto(SysEmailConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new EmailConfigListItemDto
        {
            BasicId = config.BasicId,
            ConfigCode = config.ConfigCode,
            ConfigName = config.ConfigName,
            SmtpHost = config.SmtpHost,
            SmtpPort = config.SmtpPort,
            UseSsl = config.UseSsl,
            FromEmail = config.FromEmail,
            FromName = config.FromName,
            HasPassword = !string.IsNullOrEmpty(config.Password),
            IsDefault = config.IsDefault,
            IsEnabled = config.IsEnabled,
            Sort = config.Sort,
            CreatedTime = config.CreatedTime,
            ModifiedTime = config.ModifiedTime
        };
    }

    /// <summary>
    /// 映射邮件配置详情（Password 脱敏，仅标识是否已配置）
    /// </summary>
    /// <param name="config">邮件配置</param>
    /// <returns>邮件配置详情 DTO</returns>
    public static EmailConfigDetailDto ToDetailDto(SysEmailConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new EmailConfigDetailDto
        {
            BasicId = config.BasicId,
            ConfigCode = config.ConfigCode,
            ConfigName = config.ConfigName,
            SmtpHost = config.SmtpHost,
            SmtpPort = config.SmtpPort,
            UseSsl = config.UseSsl,
            AcceptInvalidCertificate = config.AcceptInvalidCertificate,
            FromEmail = config.FromEmail,
            FromName = config.FromName,
            UserName = config.UserName,
            HasPassword = !string.IsNullOrEmpty(config.Password),
            IsBodyHtml = config.IsBodyHtml,
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
