// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Seeds;

/// <summary>
/// 平台 9 个全局消息模板的种子。
/// </summary>
/// <remarks>
/// 4.x 的 8 个：登录验证码、验证码、重置密码、注册欢迎四封邮件，登录验证码、验证码两条短信，系统通知的邮件与短信；
/// 另有系统通知的机器人消息（4.x 的通知广播直接发标题与正文，6.0 照样的文本做成模板，租户可以覆盖）。
/// 同一渠道、同一编码不存在才写入，之后由运营维护；租户可以建同编码的模板覆盖平台的。
/// </remarks>
/// <param name="templates">模板服务。</param>
public sealed class MessageTemplateSeeder(MessageTemplateService templates) : ISeeder
{
    /// <summary>
    /// 邮箱登录验证码。
    /// </summary>
    public const string EmailLoginCode = MessageTemplateCodes.EmailLoginCode;

    /// <summary>
    /// 邮箱验证码（绑定、换绑、双因素）。
    /// </summary>
    public const string EmailVerificationCode = MessageTemplateCodes.EmailVerificationCode;

    /// <summary>
    /// 重置密码邮件。
    /// </summary>
    public const string PasswordReset = "auth-password-reset";

    /// <summary>
    /// 注册欢迎邮件。
    /// </summary>
    public const string Welcome = MessageTemplateCodes.Welcome;

    /// <summary>
    /// 短信登录验证码。
    /// </summary>
    public const string SmsLoginCode = MessageTemplateCodes.SmsLoginCode;

    /// <summary>
    /// 短信验证码（绑定、换绑、双因素）。
    /// </summary>
    public const string SmsVerificationCode = MessageTemplateCodes.SmsVerificationCode;

    private const string Footer = "<div style='padding:18px 32px;background:#fafbfc;border-top:1px solid #eef0f4;font-size:12px;color:#9ca3af;text-align:center;'>本邮件由系统自动发送，请勿直接回复。</div>";

    /// <inheritdoc />
    public string Name => "notification.templates";

    /// <inheritdoc />
    public int Order => SeedOrders.PlatformData + 3;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var (code, channel, name, subject, content, html, description, order) in Definitions())
        {
            if (await templates.ExistsAsync(TenantIds.Platform, channel, code, context.CancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var created = await templates.CreateAsync(TenantIds.Platform, code, channel, new TemplateDraft(name, subject, content, html, description, IsEnabled: true, order, Remark: null), context.CancellationToken).ConfigureAwait(false);
            if (created.IsFailure)
            {
                return created.Error!;
            }
        }

        return Result.Success();
    }

    private static IEnumerable<(string Code, TemplateChannel Channel, string Name, string? Subject, string Content, bool Html, string Description, int Order)> Definitions()
    {
        yield return (EmailLoginCode, TemplateChannel.Email, "登录验证码邮件", "【{{brand}}】登录验证码", CodeEmail("登录验证码", "您正在登录 {{brand}}，请在登录页输入以下验证码完成验证："), true, "变量：code=验证码, minutes=有效分钟数, brand=品牌名", 10);
        yield return (EmailVerificationCode, TemplateChannel.Email, "验证码邮件（绑定/换绑/两步验证）", "【{{brand}}】{{title}}", CodeEmail("{{title}}", "您的验证码如下，请在对应页面输入完成验证："), true, "变量：code=验证码, minutes=有效分钟数, brand=品牌名, title=用途标题", 15);
        yield return (PasswordReset, TemplateChannel.Email, "重置密码邮件", "【{{brand}}】重置密码", Shell("重置密码", """
      <p style='margin:0 0 24px;font-size:14px;line-height:1.7;color:#6b7280;'>您申请了找回密码，请点击下方按钮设置新密码，该链接仅可使用一次：</p>
      <div style='margin:0 0 24px;text-align:center;'>
        <a href='{{reset_url}}' style='display:inline-block;padding:12px 32px;background:#4f7cff;color:#ffffff;border-radius:10px;text-decoration:none;font-size:15px;font-weight:600;'>重置密码</a>
      </div>
      <p style='margin:0 0 8px;font-size:13px;line-height:1.7;color:#6b7280;'>若按钮无法点击，请复制以下链接到浏览器打开：</p>
      <p style='margin:0 0 24px;font-size:13px;line-height:1.7;color:#3b5bdb;word-break:break-all;'>{{reset_url}}</p>
      <p style='margin:0;font-size:13px;line-height:1.7;color:#9ca3af;'>如非本人操作，请忽略本邮件，您的账号仍然安全。</p>
"""), true, "变量：reset_url=重置链接, brand=品牌名", 20);
        yield return (Welcome, TemplateChannel.Email, "注册欢迎邮件", "欢迎加入 {{brand}}", Shell("欢迎加入", """
      <p style='margin:0 0 8px;font-size:14px;line-height:1.7;color:#6b7280;'>{{user_name}}，您好！</p>
      <p style='margin:0;font-size:14px;line-height:1.7;color:#6b7280;'>您的账号已创建成功，现在即可使用注册邮箱登录 {{brand}}。</p>
"""), true, "变量：user_name=用户名, brand=品牌名", 30);
        yield return (SmsLoginCode, TemplateChannel.Sms, "登录验证码短信", null, "【{{brand}}】您的登录验证码为 {{code}}，{{minutes}} 分钟内有效，请勿泄露。", false, "变量：code=验证码, minutes=有效分钟数, brand=品牌名", 40);
        yield return (SmsVerificationCode, TemplateChannel.Sms, "验证码短信（绑定/换绑/两步验证）", null, "【{{brand}}】{{title}}：{{code}}，{{minutes}} 分钟内有效，请勿泄露。", false, "变量：code=验证码, minutes=有效分钟数, brand=品牌名, title=用途标题", 45);
        yield return (NotificationService.EmailTemplate, TemplateChannel.Email, "系统通知邮件", "【{{brand}}】{{title}}", Shell("{{title}}", """
      <div style='margin:0;font-size:14px;line-height:1.7;color:#4b5563;white-space:pre-wrap;word-break:break-word;'>{{content}}</div>
"""), true, "变量：title=通知标题, content=通知内容, link=链接, brand=品牌名", 50);
        yield return (NotificationService.SmsTemplate, TemplateChannel.Sms, "系统通知短信", null, "【{{brand}}】您有新的通知：{{title}}，详情请登录查看。", false, "变量：title=通知标题, brand=品牌名；云厂商发送须在短信配置的模板映射里登记 notification-sms 对应的服务商模板", 55);
        yield return (NotificationService.BotTemplate, TemplateChannel.Bot, "系统通知机器人消息", null, "{{title}}{{ if content }}\n\n{{content}}{{ end }}{{ if link }}\n\n{{link}}{{ end }}", false, "变量：title=通知标题, content=通知内容（没有为空）, link=链接（没有为空）, brand=品牌名", 60);
    }

    /// <summary>
    /// 验证码类邮件（全内联样式，兼容主流邮件客户端）。
    /// </summary>
    private static string CodeEmail(string heading, string lead) => Shell(heading, $$$"""
      <p style='margin:0 0 24px;font-size:14px;line-height:1.7;color:#6b7280;'>{{{lead}}}</p>
      <div style='margin:0 0 24px;padding:18px 0;text-align:center;background:#f3f6ff;border:1px solid #e3e9ff;border-radius:12px;'>
        <span style='font-size:32px;font-weight:700;letter-spacing:10px;color:#3b5bdb;'>{{code}}</span>
      </div>
      <p style='margin:0 0 8px;font-size:13px;line-height:1.7;color:#6b7280;'>验证码 <strong style='color:#374151;'>{{minutes}} 分钟</strong> 内有效，请勿向任何人泄露。</p>
      <p style='margin:0;font-size:13px;line-height:1.7;color:#9ca3af;'>如非本人操作，请忽略本邮件，您的账号仍然安全。</p>
""");

    /// <summary>
    /// 邮件外框：品牌头、标题、正文与页脚。
    /// </summary>
    /// <remarks>
    /// 4.x 同一套样式。
    /// </remarks>
    private static string Shell(string heading, string body) => $$$"""
<div style='margin:0;padding:24px;background:#f4f6fb;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <div style='max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 30px rgba(20,40,80,0.08);'>
    <div style='padding:28px 32px;background:linear-gradient(135deg,#4f7cff,#6f5bff);color:#ffffff;'>
      <div style='font-size:18px;font-weight:700;letter-spacing:.5px;'>{{brand}}</div>
    </div>
    <div style='padding:32px;'>
      <h1 style='margin:0 0 12px;font-size:20px;color:#1f2937;'>{{{heading}}}</h1>
{{{body}}}
    </div>
    {{{Footer}}}
  </div>
</div>
""";
}
