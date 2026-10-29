// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using MimeKit;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Domain.Services;

/// <summary>
/// 邮件配置内容（密码为空即保留原值）。
/// </summary>
/// <param name="Name">名称。</param>
/// <param name="SmtpHost">SMTP 主机。</param>
/// <param name="SmtpPort">端口。</param>
/// <param name="UseSsl">加密连接。</param>
/// <param name="AcceptInvalidCertificate">接受无效证书。</param>
/// <param name="FromEmail">发件地址。</param>
/// <param name="FromName">发件人名称。</param>
/// <param name="UserName">登录名。</param>
/// <param name="Password">密码。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record EmailConfigDraft(string Name, string SmtpHost, int SmtpPort, bool UseSsl, bool AcceptInvalidCertificate, string FromEmail, string? FromName, string? UserName, string? Password, bool IsEnabled, int Order, string? Remark);

/// <summary>
/// 短信配置内容（访问密钥为空即保留原值）。
/// </summary>
/// <param name="Name">名称。</param>
/// <param name="Provider">服务商。</param>
/// <param name="AccessKeyId">访问密钥 ID。</param>
/// <param name="AccessKeySecret">访问密钥。</param>
/// <param name="SdkAppId">应用 ID。</param>
/// <param name="SignName">签名。</param>
/// <param name="Region">地域。</param>
/// <param name="TemplateMap">模板映射（JSON 对象）。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record SmsConfigDraft(string Name, SmsProvider Provider, string AccessKeyId, string? AccessKeySecret, string? SdkAppId, string SignName, string? Region, string? TemplateMap, bool IsEnabled, int Order, string? Remark);

/// <summary>
/// 邮件与短信配置的规则（与存储配置同一套）：编码在同一侧唯一、建好不改；每侧至多一条缺省，第一条自动成为缺省；缺省配置须启用，不能停用或删除；
/// 发送用当前一侧的缺省配置，租户没有就用平台的。密码与访问密钥加密存储。
/// </summary>
/// <param name="emails">邮件配置仓储。</param>
/// <param name="sms">短信配置仓储。</param>
/// <param name="secrets">密钥保护。</param>
/// <param name="ids">标识生成器。</param>
public sealed class ChannelConfigService(IRepository<SysEmailConfig, Guid> emails, IRepository<SysSmsConfig, Guid> sms, ISecretProtector secrets, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 邮件密码的加密用途。
    /// </summary>
    public const string EmailSecretPurpose = "XiHan.BasicApp.Notification.EmailPassword";

    /// <summary>
    /// 短信访问密钥的加密用途。
    /// </summary>
    public const string SmsSecretPurpose = "XiHan.BasicApp.Notification.SmsSecret";

    /// <summary>
    /// 取当前一侧的邮件配置。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="id">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>配置；不存在即 <c>NOTIFICATION_NOTFOUND_004</c>。</returns>
    public async Task<Result<SysEmailConfig>> FindEmailAsync(string owner, Guid id, CancellationToken cancellationToken)
    {
        var found = await emails.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var config) && string.Equals(config.OwnerTenantId, owner, StringComparison.Ordinal)
            ? config
            : Error.NotFound(NotificationErrors.EmailConfigNotFound, NotificationMessages.EmailConfigNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 取当前一侧的短信配置。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="id">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>配置；不存在即 <c>NOTIFICATION_NOTFOUND_005</c>。</returns>
    public async Task<Result<SysSmsConfig>> FindSmsAsync(string owner, Guid id, CancellationToken cancellationToken)
    {
        var found = await sms.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var config) && string.Equals(config.OwnerTenantId, owner, StringComparison.Ordinal)
            ? config
            : Error.NotFound(NotificationErrors.SmsConfigNotFound, NotificationMessages.SmsConfigNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 发送用的邮件配置：当前一侧启用的缺省配置，没有取平台的。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>配置；都没有为 <see langword="null"/>。</returns>
    public async Task<SysEmailConfig?> ResolveEmailAsync(string owner, CancellationToken cancellationToken)
    {
        var platform = TenantIds.Platform;
        var rows = await emails.ListAsync(EntityQuery.All<SysEmailConfig>().Where(new Specification<SysEmailConfig>(c => (c.OwnerTenantId == owner || c.OwnerTenantId == platform) && c.IsDefault && c.IsEnabled)), cancellationToken).ConfigureAwait(false);
        return rows.FirstOrDefault(c => string.Equals(c.OwnerTenantId, owner, StringComparison.Ordinal)) ?? (rows.Count > 0 ? rows[0] : null);
    }

    /// <summary>
    /// 发送用的短信配置：当前一侧启用的缺省配置，没有取平台的。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>配置；都没有为 <see langword="null"/>。</returns>
    public async Task<SysSmsConfig?> ResolveSmsAsync(string owner, CancellationToken cancellationToken)
    {
        var platform = TenantIds.Platform;
        var rows = await sms.ListAsync(EntityQuery.All<SysSmsConfig>().Where(new Specification<SysSmsConfig>(c => (c.OwnerTenantId == owner || c.OwnerTenantId == platform) && c.IsDefault && c.IsEnabled)), cancellationToken).ConfigureAwait(false);
        return rows.FirstOrDefault(c => string.Equals(c.OwnerTenantId, owner, StringComparison.Ordinal)) ?? (rows.Count > 0 ? rows[0] : null);
    }

    /// <summary>
    /// 新建邮件配置（本侧还没有缺省配置即成为缺省）。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="code">编码。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新配置。</returns>
    public async Task<Result<SysEmailConfig>> CreateEmailAsync(string owner, string code, EmailConfigDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var valid = ValidateEmail(draft);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        var existing = await emails.ListAsync(EntityQuery.All<SysEmailConfig>().Where(new Specification<SysEmailConfig>(c => c.OwnerTenantId == owner)), cancellationToken).ConfigureAwait(false);
        if (existing.Any(c => string.Equals(c.Code, code, StringComparison.Ordinal)))
        {
            return CodeTaken(code);
        }

        var config = new SysEmailConfig { Id = ids.NewId(), OwnerTenantId = owner, Code = code, IsDefault = !existing.Any(static c => c.IsDefault) };
        ApplyEmail(config, draft);
        config.IsEnabled = config.IsEnabled || config.IsDefault;
        var added = await emails.AddAsync(config, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : config;
    }

    /// <summary>
    /// 修改邮件配置（编码不改；缺省配置不能停用）。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> UpdateEmailAsync(SysEmailConfig config, EmailConfigDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(draft);
        var valid = ValidateEmail(draft);
        if (valid.IsFailure)
        {
            return valid;
        }

        if (config.IsDefault && !draft.IsEnabled)
        {
            return DefaultLocked(config.Code);
        }

        ApplyEmail(config, draft);
        return await emails.UpdateAsync(config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 设为本侧的缺省邮件配置（须启用）。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> SetDefaultEmailAsync(SysEmailConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (!config.IsEnabled)
        {
            return DefaultLocked(config.Code);
        }

        var owner = config.OwnerTenantId;
        var id = config.Id;
        var others = await emails.ListAsync(EntityQuery.All<SysEmailConfig>().Where(new Specification<SysEmailConfig>(c => c.OwnerTenantId == owner && c.IsDefault && c.Id != id)), cancellationToken).ConfigureAwait(false);
        foreach (var other in others)
        {
            other.IsDefault = false;
        }

        config.IsDefault = true;
        foreach (var outcome in await emails.UpdateRangeAsync([.. others, config], cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 批量删除邮件配置；缺省配置逐项拒绝。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="configIds">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> DeleteEmailsAsync(string owner, IReadOnlyList<Guid> configIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configIds);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in configIds.Distinct())
        {
            var found = await FindEmailAsync(owner, id, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var config))
            {
                failed.Add(new BatchFailure(id, NotificationErrors.EmailConfigNotFound));
            }
            else if (config.IsDefault)
            {
                failed.Add(new BatchFailure(id, NotificationErrors.DefaultConfigLocked));
            }
            else if ((await emails.RemoveAsync(config, cancellationToken).ConfigureAwait(false)) is { IsFailure: true } removed)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
            }
            else
            {
                succeeded.Add(id);
            }
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 新建短信配置（本侧还没有缺省配置即成为缺省）。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="code">编码。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新配置。</returns>
    public async Task<Result<SysSmsConfig>> CreateSmsAsync(string owner, string code, SmsConfigDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var valid = ValidateSms(draft);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        var existing = await sms.ListAsync(EntityQuery.All<SysSmsConfig>().Where(new Specification<SysSmsConfig>(c => c.OwnerTenantId == owner)), cancellationToken).ConfigureAwait(false);
        if (existing.Any(c => string.Equals(c.Code, code, StringComparison.Ordinal)))
        {
            return CodeTaken(code);
        }

        var config = new SysSmsConfig { Id = ids.NewId(), OwnerTenantId = owner, Code = code, IsDefault = !existing.Any(static c => c.IsDefault) };
        ApplySms(config, draft);
        config.IsEnabled = config.IsEnabled || config.IsDefault;
        var added = await sms.AddAsync(config, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : config;
    }

    /// <summary>
    /// 修改短信配置（编码不改；缺省配置不能停用）。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> UpdateSmsAsync(SysSmsConfig config, SmsConfigDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(draft);
        var valid = ValidateSms(draft);
        if (valid.IsFailure)
        {
            return valid;
        }

        if (config.IsDefault && !draft.IsEnabled)
        {
            return DefaultLocked(config.Code);
        }

        ApplySms(config, draft);
        return await sms.UpdateAsync(config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 设为本侧的缺省短信配置（须启用）。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> SetDefaultSmsAsync(SysSmsConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (!config.IsEnabled)
        {
            return DefaultLocked(config.Code);
        }

        var owner = config.OwnerTenantId;
        var id = config.Id;
        var others = await sms.ListAsync(EntityQuery.All<SysSmsConfig>().Where(new Specification<SysSmsConfig>(c => c.OwnerTenantId == owner && c.IsDefault && c.Id != id)), cancellationToken).ConfigureAwait(false);
        foreach (var other in others)
        {
            other.IsDefault = false;
        }

        config.IsDefault = true;
        foreach (var outcome in await sms.UpdateRangeAsync([.. others, config], cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 批量删除短信配置；缺省配置逐项拒绝。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="configIds">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> DeleteSmsAsync(string owner, IReadOnlyList<Guid> configIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configIds);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in configIds.Distinct())
        {
            var found = await FindSmsAsync(owner, id, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var config))
            {
                failed.Add(new BatchFailure(id, NotificationErrors.SmsConfigNotFound));
            }
            else if (config.IsDefault)
            {
                failed.Add(new BatchFailure(id, NotificationErrors.DefaultConfigLocked));
            }
            else if ((await sms.RemoveAsync(config, cancellationToken).ConfigureAwait(false)) is { IsFailure: true } removed)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
            }
            else
            {
                succeeded.Add(id);
            }
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 发送用的 SMTP 服务器（密码解密；解不开按没有密码处理，由服务器拒绝认证）。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <returns>服务器。</returns>
    public EmailServer ServerOf(SysEmailConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var password = config.Password is not null && secrets.TryUnprotect(EmailSecretPurpose, config.Password, out var plain) ? plain : null;
        return new EmailServer(config.SmtpHost, config.SmtpPort, config.UseSsl, config.AcceptInvalidCertificate, config.FromEmail, config.FromName, config.UserName, password);
    }

    /// <summary>
    /// 发送用的短信账号（访问密钥解密）。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <returns>账号；密钥解不开为 <see langword="null"/>。</returns>
    public SmsAccount? AccountOf(SysSmsConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.AccessKeySecret is not null && secrets.TryUnprotect(SmsSecretPurpose, config.AccessKeySecret, out var secret)
            ? new SmsAccount(config.Provider, config.AccessKeyId, secret, config.SdkAppId, config.SignName, config.Region)
            : null;
    }

    /// <summary>
    /// 模板编码在服务商那边的模板 ID（短信配置的模板映射）。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="templateCode">本应用的模板编码。</param>
    /// <returns>服务商的模板 ID；没映射为 <see langword="null"/>。</returns>
    public static string? ProviderTemplateOf(SysSmsConfig config, string templateCode)
    {
        ArgumentNullException.ThrowIfNull(config);
        var map = ParseTemplateMap(config.TemplateMap);
        return map is not null && map.TryGetValue(templateCode, out var id) && !string.IsNullOrWhiteSpace(id) ? id : null;
    }

    private static Dictionary<string, string>? ParseTemplateMap(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize(json, NotificationJsonContext.Default.DictionaryStringString);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Result ValidateEmail(EmailConfigDraft draft)
        // MimeKit 认只有本地部分的地址（「nope」），发件地址要有域名、不带显示名（显示名另填）
        => MailboxAddress.TryParse(draft.FromEmail, out var mailbox) && !string.IsNullOrEmpty(mailbox.Domain) && string.IsNullOrEmpty(mailbox.Name)
            ? Result.Success()
            : Error.Validation(NotificationErrors.EmailAddressInvalid, NotificationMessages.EmailAddressInvalid, MessageArguments.Of(("address", draft.FromEmail)));

    private static Result ValidateSms(SmsConfigDraft draft)
        => ParseTemplateMap(draft.TemplateMap) is null
            ? Error.Validation(NotificationErrors.TemplateMapInvalid, NotificationMessages.TemplateMapInvalid)
            : Result.Success();

    private static Error CodeTaken(string code) => Error.Conflict(NotificationErrors.ConfigCodeTaken, NotificationMessages.ConfigCodeTaken, MessageArguments.Of(("code", code)));

    private static Error DefaultLocked(string code) => Error.Conflict(NotificationErrors.DefaultConfigLocked, NotificationMessages.DefaultConfigLocked, MessageArguments.Of(("code", code)));

    private void ApplyEmail(SysEmailConfig config, EmailConfigDraft draft)
    {
        config.Name = draft.Name.Trim();
        config.SmtpHost = draft.SmtpHost.Trim();
        config.SmtpPort = draft.SmtpPort;
        config.UseSsl = draft.UseSsl;
        config.AcceptInvalidCertificate = draft.AcceptInvalidCertificate;
        config.FromEmail = draft.FromEmail.Trim();
        config.FromName = string.IsNullOrWhiteSpace(draft.FromName) ? null : draft.FromName.Trim();
        config.UserName = string.IsNullOrWhiteSpace(draft.UserName) ? null : draft.UserName.Trim();
        if (!string.IsNullOrEmpty(draft.Password))
        {
            config.Password = secrets.Protect(EmailSecretPurpose, draft.Password);
        }

        config.IsEnabled = draft.IsEnabled;
        config.Order = draft.Order;
        config.Remark = string.IsNullOrWhiteSpace(draft.Remark) ? null : draft.Remark.Trim();
    }

    private void ApplySms(SysSmsConfig config, SmsConfigDraft draft)
    {
        config.Name = draft.Name.Trim();
        config.Provider = draft.Provider;
        config.AccessKeyId = draft.AccessKeyId.Trim();
        if (!string.IsNullOrEmpty(draft.AccessKeySecret))
        {
            config.AccessKeySecret = secrets.Protect(SmsSecretPurpose, draft.AccessKeySecret);
        }

        config.SdkAppId = string.IsNullOrWhiteSpace(draft.SdkAppId) ? null : draft.SdkAppId.Trim();
        config.SignName = draft.SignName.Trim();
        config.Region = string.IsNullOrWhiteSpace(draft.Region) ? null : draft.Region.Trim();
        config.TemplateMap = string.IsNullOrWhiteSpace(draft.TemplateMap) ? null : draft.TemplateMap;
        config.IsEnabled = draft.IsEnabled;
        config.Order = draft.Order;
        config.Remark = string.IsNullOrWhiteSpace(draft.Remark) ? null : draft.Remark.Trim();
    }
}
