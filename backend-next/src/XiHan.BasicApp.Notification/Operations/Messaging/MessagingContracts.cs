// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;

namespace XiHan.BasicApp.Notification.Operations.Messaging;

/// <summary>
/// 编码的文法：小写字母或数字开头，只含小写字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 64。
/// </summary>
public static class MessagingCodes
{
    /// <summary>
    /// 正则。
    /// </summary>
    public const string Pattern = "^[a-z0-9][a-z0-9._-]{0,63}$";
}

/// <summary>
/// 一个消息模板。
/// </summary>
/// <param name="Id">模板 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Channel">渠道。</param>
/// <param name="Name">名称。</param>
/// <param name="Subject">主题。</param>
/// <param name="Content">正文。</param>
/// <param name="IsHtml">正文是 HTML。</param>
/// <param name="Description">说明。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record MessageTemplateResponse(Guid Id, string Code, TemplateChannel Channel, string Name, string? Subject, string Content, bool IsHtml, string? Description, bool IsEnabled, int Order, string? Remark, DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="t">模板。</param>
    /// <returns>响应。</returns>
    public static MessageTemplateResponse From(SysMessageTemplate t)
    {
        ArgumentNullException.ThrowIfNull(t);
        return new(t.Id, t.Code, t.Channel, t.Name, t.Subject, t.Content, t.IsHtml, t.Description, t.IsEnabled, t.Order, t.Remark, t.CreatedAt, t.ModifiedAt);
    }
}

/// <summary>
/// <c>message-templates.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（建好不改）。</param>
/// <param name="Channel">渠道（建好不改）。</param>
/// <param name="Name">名称。</param>
/// <param name="Content">正文。</param>
/// <param name="Subject">主题（邮件）。</param>
/// <param name="IsHtml">正文是 HTML。</param>
/// <param name="Description">说明。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateMessageTemplateRequest(
    [Required, StringLength(64, MinimumLength = 1), RegularExpression(MessagingCodes.Pattern)] string Code,
    TemplateChannel Channel,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    [Required, StringLength(100_000, MinimumLength = 1)] string Content,
    [StringLength(256)] string? Subject = null,
    bool IsHtml = false,
    [StringLength(512)] string? Description = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public TemplateDraft ToDraft() => new(Name, Subject, Content, IsHtml, Description, IsEnabled, Order, Remark);
}

/// <summary>
/// <c>message-templates.update</c> 的请求。
/// </summary>
/// <remarks>
/// 编码与渠道不改。标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">模板 ID。</param>
/// <param name="Name">名称。</param>
/// <param name="Content">正文。</param>
/// <param name="Subject">主题。</param>
/// <param name="IsHtml">正文是 HTML。</param>
/// <param name="Description">说明。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateMessageTemplateRequest(
    Guid Id,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    [Required, StringLength(100_000, MinimumLength = 1)] string Content,
    [StringLength(256)] string? Subject = null,
    bool IsHtml = false,
    [StringLength(512)] string? Description = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public TemplateDraft ToDraft() => new(Name, Subject, Content, IsHtml, Description, IsEnabled, Order, Remark);
}

/// <summary>
/// 一个邮件配置（密码不回显）。
/// </summary>
/// <param name="Id">配置 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="SmtpHost">SMTP 主机。</param>
/// <param name="SmtpPort">端口。</param>
/// <param name="UseSsl">加密连接。</param>
/// <param name="AcceptInvalidCertificate">接受无效证书。</param>
/// <param name="FromEmail">发件地址。</param>
/// <param name="FromName">发件人名称。</param>
/// <param name="UserName">登录名。</param>
/// <param name="HasPassword">是否存了密码。</param>
/// <param name="IsDefault">是否缺省。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record EmailConfigResponse(Guid Id, string Code, string Name, string SmtpHost, int SmtpPort, bool UseSsl, bool AcceptInvalidCertificate, string FromEmail, string? FromName, string? UserName, bool HasPassword, bool IsDefault, bool IsEnabled, int Order, string? Remark, DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="c">配置。</param>
    /// <returns>响应。</returns>
    public static EmailConfigResponse From(SysEmailConfig c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new(c.Id, c.Code, c.Name, c.SmtpHost, c.SmtpPort, c.UseSsl, c.AcceptInvalidCertificate, c.FromEmail, c.FromName, c.UserName, c.Password is not null, c.IsDefault, c.IsEnabled, c.Order, c.Remark, c.CreatedAt, c.ModifiedAt);
    }
}

/// <summary>
/// <c>email-configs.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（建好不改）。</param>
/// <param name="Name">名称。</param>
/// <param name="SmtpHost">SMTP 主机。</param>
/// <param name="FromEmail">发件地址。</param>
/// <param name="SmtpPort">端口。</param>
/// <param name="UseSsl">加密连接。</param>
/// <param name="AcceptInvalidCertificate">接受无效证书。</param>
/// <param name="FromName">发件人名称。</param>
/// <param name="UserName">登录名。</param>
/// <param name="Password">密码。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateEmailConfigRequest(
    [Required, StringLength(64, MinimumLength = 1), RegularExpression(MessagingCodes.Pattern)] string Code,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    [Required, StringLength(256, MinimumLength = 1)] string SmtpHost,
    [Required, StringLength(256, MinimumLength = 3)] string FromEmail,
    [Range(1, 65535)] int SmtpPort = 587,
    bool UseSsl = true,
    bool AcceptInvalidCertificate = false,
    [StringLength(128)] string? FromName = null,
    [StringLength(256)] string? UserName = null,
    [StringLength(512)] string? Password = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public EmailConfigDraft ToDraft() => new(Name, SmtpHost, SmtpPort, UseSsl, AcceptInvalidCertificate, FromEmail, FromName, UserName, Password, IsEnabled, Order, Remark);
}

/// <summary>
/// <c>email-configs.update</c> 的请求。
/// </summary>
/// <remarks>
/// 编码不改，密码不填即保留。标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">配置 ID。</param>
/// <param name="Name">名称。</param>
/// <param name="SmtpHost">SMTP 主机。</param>
/// <param name="FromEmail">发件地址。</param>
/// <param name="SmtpPort">端口。</param>
/// <param name="UseSsl">加密连接。</param>
/// <param name="AcceptInvalidCertificate">接受无效证书。</param>
/// <param name="FromName">发件人名称。</param>
/// <param name="UserName">登录名。</param>
/// <param name="Password">密码。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateEmailConfigRequest(
    Guid Id,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    [Required, StringLength(256, MinimumLength = 1)] string SmtpHost,
    [Required, StringLength(256, MinimumLength = 3)] string FromEmail,
    [Range(1, 65535)] int SmtpPort = 587,
    bool UseSsl = true,
    bool AcceptInvalidCertificate = false,
    [StringLength(128)] string? FromName = null,
    [StringLength(256)] string? UserName = null,
    [StringLength(512)] string? Password = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public EmailConfigDraft ToDraft() => new(Name, SmtpHost, SmtpPort, UseSsl, AcceptInvalidCertificate, FromEmail, FromName, UserName, Password, IsEnabled, Order, Remark);
}

/// <summary>
/// 一个短信配置（访问密钥不回显）。
/// </summary>
/// <param name="Id">配置 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Provider">服务商。</param>
/// <param name="AccessKeyId">访问密钥 ID。</param>
/// <param name="HasAccessKeySecret">是否存了访问密钥。</param>
/// <param name="SdkAppId">应用 ID。</param>
/// <param name="SignName">签名。</param>
/// <param name="Region">地域。</param>
/// <param name="TemplateMap">模板映射（JSON 对象）。</param>
/// <param name="IsDefault">是否缺省。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record SmsConfigResponse(Guid Id, string Code, string Name, SmsProvider Provider, string AccessKeyId, bool HasAccessKeySecret, string? SdkAppId, string SignName, string? Region, string? TemplateMap, bool IsDefault, bool IsEnabled, int Order, string? Remark, DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="c">配置。</param>
    /// <returns>响应。</returns>
    public static SmsConfigResponse From(SysSmsConfig c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new(c.Id, c.Code, c.Name, c.Provider, c.AccessKeyId, c.AccessKeySecret is not null, c.SdkAppId, c.SignName, c.Region, c.TemplateMap, c.IsDefault, c.IsEnabled, c.Order, c.Remark, c.CreatedAt, c.ModifiedAt);
    }
}

/// <summary>
/// <c>sms-configs.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（建好不改）。</param>
/// <param name="Name">名称。</param>
/// <param name="Provider">服务商。</param>
/// <param name="AccessKeyId">访问密钥 ID。</param>
/// <param name="SignName">签名。</param>
/// <param name="AccessKeySecret">访问密钥。</param>
/// <param name="SdkAppId">应用 ID（腾讯云）。</param>
/// <param name="Region">地域。</param>
/// <param name="TemplateMap">模板映射（JSON 对象：模板编码 → 服务商模板 ID）。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateSmsConfigRequest(
    [Required, StringLength(64, MinimumLength = 1), RegularExpression(MessagingCodes.Pattern)] string Code,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    SmsProvider Provider,
    [Required, StringLength(256, MinimumLength = 1)] string AccessKeyId,
    [Required, StringLength(64, MinimumLength = 1)] string SignName,
    [StringLength(512)] string? AccessKeySecret = null,
    [StringLength(64)] string? SdkAppId = null,
    [StringLength(64)] string? Region = null,
    [StringLength(20_000)] string? TemplateMap = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public SmsConfigDraft ToDraft() => new(Name, Provider, AccessKeyId, AccessKeySecret, SdkAppId, SignName, Region, TemplateMap, IsEnabled, Order, Remark);
}

/// <summary>
/// <c>sms-configs.update</c> 的请求。
/// </summary>
/// <remarks>
/// 编码不改，访问密钥不填即保留。标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">配置 ID。</param>
/// <param name="Name">名称。</param>
/// <param name="Provider">服务商。</param>
/// <param name="AccessKeyId">访问密钥 ID。</param>
/// <param name="SignName">签名。</param>
/// <param name="AccessKeySecret">访问密钥。</param>
/// <param name="SdkAppId">应用 ID。</param>
/// <param name="Region">地域。</param>
/// <param name="TemplateMap">模板映射。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateSmsConfigRequest(
    Guid Id,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    SmsProvider Provider,
    [Required, StringLength(256, MinimumLength = 1)] string AccessKeyId,
    [Required, StringLength(64, MinimumLength = 1)] string SignName,
    [StringLength(512)] string? AccessKeySecret = null,
    [StringLength(64)] string? SdkAppId = null,
    [StringLength(64)] string? Region = null,
    [StringLength(20_000)] string? TemplateMap = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public SmsConfigDraft ToDraft() => new(Name, Provider, AccessKeyId, AccessKeySecret, SdkAppId, SignName, Region, TemplateMap, IsEnabled, Order, Remark);
}

/// <summary>
/// 一条邮件发送记录。
/// </summary>
/// <param name="Id">记录 ID。</param>
/// <param name="ToEmail">收件地址。</param>
/// <param name="Subject">主题。</param>
/// <param name="Content">正文。</param>
/// <param name="IsHtml">正文是 HTML。</param>
/// <param name="TemplateCode">模板编码。</param>
/// <param name="Status">状态。</param>
/// <param name="Attempts">已尝试次数。</param>
/// <param name="MaxAttempts">最多尝试次数。</param>
/// <param name="NextAttemptAt">下次尝试时间。</param>
/// <param name="SentAt">发出时间。</param>
/// <param name="ErrorMessage">失败原因。</param>
/// <param name="ConfigId">用的配置。</param>
/// <param name="SenderId">发起人。</param>
/// <param name="ReceiverId">接收人。</param>
/// <param name="BusinessType">业务类型。</param>
/// <param name="BusinessId">业务 ID。</param>
/// <param name="CreatedAt">写入时间。</param>
public sealed record EmailRecordResponse(Guid Id, string ToEmail, string Subject, string? Content, bool IsHtml, string? TemplateCode, MessageSendStatus Status, int Attempts, int MaxAttempts, DateTimeOffset? NextAttemptAt, DateTimeOffset? SentAt, string? ErrorMessage, Guid? ConfigId, Guid? SenderId, Guid? ReceiverId, string? BusinessType, string? BusinessId, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="e">记录。</param>
    /// <returns>响应。</returns>
    public static EmailRecordResponse From(SysEmail e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new(e.Id, e.ToEmail, e.Subject, e.Content, e.IsHtml, e.TemplateCode, e.Status, e.Attempts, e.MaxAttempts, e.NextAttemptAt, e.SentAt, e.ErrorMessage, e.ConfigId, e.SenderId, e.ReceiverId, e.BusinessType, e.BusinessId, e.CreatedAt);
    }
}

/// <summary>
/// 一条短信发送记录。
/// </summary>
/// <param name="Id">记录 ID。</param>
/// <param name="ToPhone">手机号。</param>
/// <param name="Content">正文。</param>
/// <param name="TemplateCode">模板编码。</param>
/// <param name="TemplateParams">模板参数（JSON）。</param>
/// <param name="Provider">服务商。</param>
/// <param name="ProviderMessageId">服务商消息 ID。</param>
/// <param name="Status">状态。</param>
/// <param name="Attempts">已尝试次数。</param>
/// <param name="MaxAttempts">最多尝试次数。</param>
/// <param name="NextAttemptAt">下次尝试时间。</param>
/// <param name="SentAt">发出时间。</param>
/// <param name="ErrorMessage">失败原因。</param>
/// <param name="ConfigId">用的配置。</param>
/// <param name="SenderId">发起人。</param>
/// <param name="ReceiverId">接收人。</param>
/// <param name="BusinessType">业务类型。</param>
/// <param name="BusinessId">业务 ID。</param>
/// <param name="CreatedAt">写入时间。</param>
public sealed record SmsRecordResponse(Guid Id, string ToPhone, string Content, string? TemplateCode, string? TemplateParams, SmsProvider? Provider, string? ProviderMessageId, MessageSendStatus Status, int Attempts, int MaxAttempts, DateTimeOffset? NextAttemptAt, DateTimeOffset? SentAt, string? ErrorMessage, Guid? ConfigId, Guid? SenderId, Guid? ReceiverId, string? BusinessType, string? BusinessId, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="s">记录。</param>
    /// <returns>响应。</returns>
    public static SmsRecordResponse From(SysSms s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new(s.Id, s.ToPhone, s.Content, s.TemplateCode, s.TemplateParams, s.Provider, s.ProviderMessageId, s.Status, s.Attempts, s.MaxAttempts, s.NextAttemptAt, s.SentAt, s.ErrorMessage, s.ConfigId, s.SenderId, s.ReceiverId, s.BusinessType, s.BusinessId, s.CreatedAt);
    }
}

/// <summary>
/// 按 ID 操作的请求。
/// </summary>
/// <remarks>
/// 标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">ID。</param>
public sealed record MessagingIdRequest(Guid Id);

/// <summary>
/// 批量操作的请求。
/// </summary>
/// <param name="Ids">ID，1 到 100 个。</param>
public sealed record MessagingIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
