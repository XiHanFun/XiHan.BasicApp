#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailConfigDomainService
// Guid:6c3a8e17-4d95-4b28-8a6c-2e7f5b9d1c40
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 邮件网关配置领域服务实现
/// </summary>
public sealed class EmailConfigDomainService
    : IEmailConfigDomainService
{
    private readonly IEmailConfigRepository _emailConfigRepository;

    private readonly IEmailConfigSecretProtector _secretProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmailConfigDomainService(
        IEmailConfigRepository emailConfigRepository,
        IEmailConfigSecretProtector secretProtector)
    {
        _emailConfigRepository = emailConfigRepository;
        _secretProtector = secretProtector;
    }

    /// <inheritdoc />
    public async Task<EmailConfigCommandResult> CreateEmailConfigAsync(EmailConfigCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);
        var configCode = command.ConfigCode.Trim();
        if (await _emailConfigRepository.AnyAsync(config => config.ConfigCode == configCode, cancellationToken))
        {
            throw new InvalidOperationException("邮件网关配置编码已存在。");
        }

        if (command.IsDefault)
        {
            EnsureEnabledDefault(command.IsEnabled);
            await ClearDefaultConfigsAsync(null, cancellationToken);
        }

        var config = new SysEmailConfig
        {
            ConfigCode = configCode,
            ConfigName = command.ConfigName.Trim(),
            SmtpHost = command.SmtpHost.Trim(),
            SmtpPort = command.SmtpPort,
            UseSsl = command.UseSsl,
            AcceptInvalidCertificate = command.AcceptInvalidCertificate,
            FromEmail = command.FromEmail.Trim(),
            FromName = command.FromName.Trim(),
            UserName = NormalizeNullable(command.UserName),
            Password = _secretProtector.Protect(NormalizeNullable(command.Password)),
            IsBodyHtml = command.IsBodyHtml,
            IsDefault = command.IsDefault,
            IsEnabled = command.IsEnabled,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        return new EmailConfigCommandResult(await _emailConfigRepository.AddAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<EmailConfigCommandResult> UpdateEmailConfigAsync(EmailConfigUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);
        var config = await GetEmailConfigOrThrowAsync(command.BasicId, cancellationToken);

        config.ConfigName = command.ConfigName.Trim();
        config.SmtpHost = command.SmtpHost.Trim();
        config.SmtpPort = command.SmtpPort;
        config.UseSsl = command.UseSsl;
        config.AcceptInvalidCertificate = command.AcceptInvalidCertificate;
        config.FromEmail = command.FromEmail.Trim();
        config.FromName = command.FromName.Trim();
        config.UserName = NormalizeNullable(command.UserName);
        config.IsBodyHtml = command.IsBodyHtml;
        config.Sort = command.Sort;
        config.Remark = NormalizeNullable(command.Remark);

        // 密码为空表示保留原密码（前端脱敏不回显）；提供则加密落库
        var password = NormalizeNullable(command.Password);
        if (password is not null)
        {
            config.Password = _secretProtector.Protect(password);
        }

        return new EmailConfigCommandResult(await _emailConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<EmailConfigCommandResult> UpdateEmailConfigStatusAsync(EmailConfigStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "邮件网关配置主键必须大于 0。");
        var config = await GetEmailConfigOrThrowAsync(command.BasicId, cancellationToken);
        if (config.IsDefault && !command.IsEnabled)
        {
            throw new InvalidOperationException("默认邮件网关配置不能停用，请先将其他启用配置设为默认。");
        }

        config.IsEnabled = command.IsEnabled;
        return new EmailConfigCommandResult(await _emailConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<EmailConfigCommandResult> SetDefaultEmailConfigAsync(EmailConfigDefaultChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "邮件网关配置主键必须大于 0。");
        var config = await GetEmailConfigOrThrowAsync(command.BasicId, cancellationToken);
        EnsureEnabledDefault(config.IsEnabled);

        await ClearDefaultConfigsAsync(config.BasicId, cancellationToken);
        config.IsDefault = true;

        return new EmailConfigCommandResult(await _emailConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<EmailConfigCommandResult> DeleteEmailConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = await GetEmailConfigOrThrowAsync(id, cancellationToken);
        if (config.IsDefault)
        {
            throw new InvalidOperationException("默认邮件网关配置不能删除，请先将其他配置设为默认。");
        }

        if (!await _emailConfigRepository.DeleteAsync(config, cancellationToken))
        {
            throw new InvalidOperationException("邮件网关配置删除失败。");
        }

        return new EmailConfigCommandResult(config);
    }

    private static void EnsureEnabledDefault(bool isEnabled)
    {
        if (!isEnabled)
        {
            throw new InvalidOperationException("默认邮件网关配置必须处于启用状态。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateCommonInput(int smtpPort, string fromEmail, int sort)
    {
        if (smtpPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(smtpPort), "SMTP 端口必须在 1-65535 之间。");
        }

        if (!fromEmail.Contains('@'))
        {
            throw new InvalidOperationException("发件邮箱格式无效。");
        }

        if (sort < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sort), "排序不能小于 0。");
        }
    }

    private static void ValidateCreateCommand(EmailConfigCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SmtpHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.FromEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.FromName);
        ValidateCommonInput(command.SmtpPort, command.FromEmail.Trim(), command.Sort);
    }

    private static void ValidateUpdateCommand(EmailConfigUpdateCommand command)
    {
        EnsureId(command.BasicId, "邮件网关配置主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SmtpHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.FromEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.FromName);
        ValidateCommonInput(command.SmtpPort, command.FromEmail.Trim(), command.Sort);
    }

    private Task<bool> ClearDefaultConfigsAsync(long? excludeId, CancellationToken cancellationToken)
    {
        return excludeId.HasValue
            ? _emailConfigRepository.UpdateAsync(
                config => new SysEmailConfig { IsDefault = false },
                config => config.IsDefault && config.BasicId != excludeId.Value,
                cancellationToken)
            : _emailConfigRepository.UpdateAsync(
                config => new SysEmailConfig { IsDefault = false },
                config => config.IsDefault,
                cancellationToken);
    }

    private async Task<SysEmailConfig> GetEmailConfigOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "邮件网关配置主键必须大于 0。");
        return await _emailConfigRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("邮件网关配置不存在。");
    }
}
