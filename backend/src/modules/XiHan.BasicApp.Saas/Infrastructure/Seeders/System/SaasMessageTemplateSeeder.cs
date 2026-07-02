#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasMessageTemplateSeeder
// Guid:ea936b8b-cf2a-45d6-b378-afbf2e9ffcb3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Messaging;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 消息模板种子数据（平台全局默认模板，租户可建同编码模板覆盖）
/// </summary>
/// <remarks>
/// 占位符语法须与默认模板引擎（DefaultTemplateEngine）一致：用 <c>{{key}}</c>（无空格）。变量名见各模板 Description。
/// </remarks>
public sealed class SaasMessageTemplateSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasMessageTemplateSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string SeededRemark = "系统初始化全局消息模板";
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级（晚于权限/菜单，无依赖）
    /// </summary>
    public override int Order => 27;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]消息模板种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var definitions = BuildDefinitions();
        var codes = definitions.Select(definition => definition.TemplateCode).ToArray();
        var existing = await client.Queryable<SysMessageTemplate>()
            .Where(template => template.TenantId == 0 && codes.Contains(template.TemplateCode))
            .ToListAsync();
        var map = existing.ToDictionary(template => (template.Channel, template.TemplateCode));

        var addCount = 0;
        foreach (var definition in definitions)
        {
            if (map.ContainsKey((definition.Channel, definition.TemplateCode)))
            {
                // 已存在不覆盖：模板内容允许运营修改，种子只负责首次落地
                continue;
            }

            var template = new SysMessageTemplate
            {
                TenantId = 0,
                TemplateCode = definition.TemplateCode,
                Channel = definition.Channel,
                TemplateName = definition.TemplateName,
                Subject = definition.Subject,
                Content = definition.Content,
                IsHtml = definition.IsHtml,
                Description = definition.Description,
                Status = EnableStatus.Enabled,
                Sort = definition.Sort,
                Remark = SeededRemark
            };
            _ = await client.Insertable(template).ExecuteReturnEntityAsync();
            addCount++;
        }

        if (addCount > 0)
        {
            Logger.LogInformation("成功初始化 SaaS 消息模板，新增 {AddCount} 个", addCount);
        }
        else
        {
            Logger.LogInformation("SaaS 消息模板数据已存在，跳过种子数据");
        }
    }

    private static IReadOnlyList<TemplateSeedDefinition> BuildDefinitions()
    {
        return
        [
            new(
                SaasMessageTemplateCodes.Auth.EmailLoginCode,
                MessageChannel.Email,
                "登录验证码邮件",
                "【{{brand}}】登录验证码",
                BuildCodeEmailHtml("登录验证码", "您正在登录 {{brand}}，请在登录页输入以下验证码完成验证："),
                IsHtml: true,
                "变量：code=验证码, minutes=有效分钟数, brand=品牌名",
                10),
            new(
                SaasMessageTemplateCodes.Auth.EmailVerificationCode,
                MessageChannel.Email,
                "验证码邮件（绑定/换绑/两步验证）",
                "【{{brand}}】{{title}}",
                BuildCodeEmailHtml("{{title}}", "您的验证码如下，请在对应页面输入完成验证："),
                IsHtml: true,
                "变量：code=验证码, minutes=有效分钟数, brand=品牌名, title=用途标题",
                15),
            new(
                SaasMessageTemplateCodes.Auth.PasswordReset,
                MessageChannel.Email,
                "重置密码邮件",
                "【{{brand}}】重置密码",
                BuildPasswordResetEmailHtml(),
                IsHtml: true,
                "变量：reset_url=重置链接, brand=品牌名",
                20),
            new(
                SaasMessageTemplateCodes.Auth.Welcome,
                MessageChannel.Email,
                "注册欢迎邮件",
                "欢迎加入 {{brand}}",
                BuildWelcomeEmailHtml(),
                IsHtml: true,
                "变量：user_name=用户名, brand=品牌名",
                30),
            new(
                SaasMessageTemplateCodes.Auth.SmsLoginCode,
                MessageChannel.Sms,
                "登录验证码短信",
                Subject: null,
                "【{{brand}}】您的登录验证码为 {{code}}，{{minutes}} 分钟内有效，请勿泄露。",
                IsHtml: false,
                "变量：code=验证码, minutes=有效分钟数, brand=品牌名",
                40),
            new(
                SaasMessageTemplateCodes.Auth.SmsVerificationCode,
                MessageChannel.Sms,
                "验证码短信（绑定/换绑/两步验证）",
                Subject: null,
                "【{{brand}}】{{title}}：{{code}}，{{minutes}} 分钟内有效，请勿泄露。",
                IsHtml: false,
                "变量：code=验证码, minutes=有效分钟数, brand=品牌名, title=用途标题",
                45),
            new(
                SaasMessageTemplateCodes.Notification.Email,
                MessageChannel.Email,
                "系统通知邮件",
                "【{{brand}}】{{title}}",
                BuildNotificationEmailHtml(),
                IsHtml: true,
                "变量：title=通知标题, content=通知内容, brand=品牌名",
                50),
            new(
                SaasMessageTemplateCodes.Notification.Sms,
                MessageChannel.Sms,
                "系统通知短信",
                Subject: null,
                "【{{brand}}】您有新的通知：{{title}}，详情请登录查看。",
                IsHtml: false,
                "变量：title=通知标题, brand=品牌名；云厂商发送须在短信配置 TemplateMap 登记 notification-sms → 服务商模板码的映射",
                55),
        ];
    }

    /// <summary>
    /// 验证码类邮件通用 HTML（全内联样式，兼容主流邮件客户端；占位符 {{code}}/{{minutes}}/{{brand}}）
    /// </summary>
    private static string BuildCodeEmailHtml(string heading, string lead)
    {
        return $@"<div style='margin:0;padding:24px;background:#f4f6fb;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <div style='max-width:480px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 30px rgba(20,40,80,0.08);'>
    <div style='padding:28px 32px;background:linear-gradient(135deg,#4f7cff,#6f5bff);color:#ffffff;'>
      <div style='font-size:18px;font-weight:700;letter-spacing:.5px;'>{{{{brand}}}}</div>
    </div>
    <div style='padding:32px;'>
      <h1 style='margin:0 0 12px;font-size:20px;color:#1f2937;'>{heading}</h1>
      <p style='margin:0 0 24px;font-size:14px;line-height:1.7;color:#6b7280;'>{lead}</p>
      <div style='margin:0 0 24px;padding:18px 0;text-align:center;background:#f3f6ff;border:1px solid #e3e9ff;border-radius:12px;'>
        <span style='font-size:32px;font-weight:700;letter-spacing:10px;color:#3b5bdb;'>{{{{code}}}}</span>
      </div>
      <p style='margin:0 0 8px;font-size:13px;line-height:1.7;color:#6b7280;'>验证码 <strong style='color:#374151;'>{{{{minutes}}}} 分钟</strong> 内有效，请勿向任何人泄露。</p>
      <p style='margin:0;font-size:13px;line-height:1.7;color:#9ca3af;'>如非本人操作，请忽略本邮件，您的账号仍然安全。</p>
    </div>
    <div style='padding:18px 32px;background:#fafbfc;border-top:1px solid #eef0f4;font-size:12px;color:#9ca3af;text-align:center;'>本邮件由系统自动发送，请勿直接回复。</div>
  </div>
</div>";
    }

    /// <summary>
    /// 重置密码邮件 HTML（链接版；占位符 {{reset_url}}/{{brand}}）
    /// </summary>
    private static string BuildPasswordResetEmailHtml()
    {
        return @"<div style='margin:0;padding:24px;background:#f4f6fb;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <div style='max-width:480px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 30px rgba(20,40,80,0.08);'>
    <div style='padding:28px 32px;background:linear-gradient(135deg,#4f7cff,#6f5bff);color:#ffffff;'>
      <div style='font-size:18px;font-weight:700;letter-spacing:.5px;'>{{brand}}</div>
    </div>
    <div style='padding:32px;'>
      <h1 style='margin:0 0 12px;font-size:20px;color:#1f2937;'>重置密码</h1>
      <p style='margin:0 0 24px;font-size:14px;line-height:1.7;color:#6b7280;'>您申请了找回密码，请点击下方按钮设置新密码，该链接仅可使用一次：</p>
      <div style='margin:0 0 24px;text-align:center;'>
        <a href='{{reset_url}}' style='display:inline-block;padding:12px 32px;background:#4f7cff;color:#ffffff;border-radius:10px;text-decoration:none;font-size:15px;font-weight:600;'>重置密码</a>
      </div>
      <p style='margin:0 0 8px;font-size:13px;line-height:1.7;color:#6b7280;'>若按钮无法点击，请复制以下链接到浏览器打开：</p>
      <p style='margin:0 0 24px;font-size:13px;line-height:1.7;color:#3b5bdb;word-break:break-all;'>{{reset_url}}</p>
      <p style='margin:0;font-size:13px;line-height:1.7;color:#9ca3af;'>如非本人操作，请忽略本邮件，您的账号仍然安全。</p>
    </div>
    <div style='padding:18px 32px;background:#fafbfc;border-top:1px solid #eef0f4;font-size:12px;color:#9ca3af;text-align:center;'>本邮件由系统自动发送，请勿直接回复。</div>
  </div>
</div>";
    }

    /// <summary>
    /// 系统通知邮件 HTML（占位符 {{title}}/{{content}}/{{brand}}；content 以 pre-wrap 呈现保留纯文本/Markdown 换行，HTML 内容亦可正常渲染）
    /// </summary>
    private static string BuildNotificationEmailHtml()
    {
        return @"<div style='margin:0;padding:24px;background:#f4f6fb;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <div style='max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 30px rgba(20,40,80,0.08);'>
    <div style='padding:28px 32px;background:linear-gradient(135deg,#4f7cff,#6f5bff);color:#ffffff;'>
      <div style='font-size:18px;font-weight:700;letter-spacing:.5px;'>{{brand}}</div>
    </div>
    <div style='padding:32px;'>
      <h1 style='margin:0 0 16px;font-size:20px;color:#1f2937;'>{{title}}</h1>
      <div style='margin:0;font-size:14px;line-height:1.7;color:#4b5563;white-space:pre-wrap;word-break:break-word;'>{{content}}</div>
    </div>
    <div style='padding:18px 32px;background:#fafbfc;border-top:1px solid #eef0f4;font-size:12px;color:#9ca3af;text-align:center;'>本邮件由系统自动发送，请勿直接回复。详情请登录系统查看。</div>
  </div>
</div>";
    }

    private static string BuildWelcomeEmailHtml()
    {
        return @"<div style='margin:0;padding:24px;background:#f4f6fb;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <div style='max-width:480px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 30px rgba(20,40,80,0.08);'>
    <div style='padding:28px 32px;background:linear-gradient(135deg,#4f7cff,#6f5bff);color:#ffffff;'>
      <div style='font-size:18px;font-weight:700;letter-spacing:.5px;'>{{brand}}</div>
    </div>
    <div style='padding:32px;'>
      <h1 style='margin:0 0 12px;font-size:20px;color:#1f2937;'>欢迎加入</h1>
      <p style='margin:0 0 8px;font-size:14px;line-height:1.7;color:#6b7280;'>{{user_name}}，您好！</p>
      <p style='margin:0;font-size:14px;line-height:1.7;color:#6b7280;'>您的账号已创建成功，现在即可使用注册邮箱登录 {{brand}}。</p>
    </div>
    <div style='padding:18px 32px;background:#fafbfc;border-top:1px solid #eef0f4;font-size:12px;color:#9ca3af;text-align:center;'>本邮件由系统自动发送，请勿直接回复。</div>
  </div>
</div>";
    }

    private sealed record TemplateSeedDefinition(
        string TemplateCode,
        MessageChannel Channel,
        string TemplateName,
        string? Subject,
        string Content,
        bool IsHtml,
        string Description,
        int Sort);
}
