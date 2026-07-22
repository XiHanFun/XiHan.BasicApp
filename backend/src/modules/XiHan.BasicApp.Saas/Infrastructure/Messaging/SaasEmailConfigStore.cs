// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Bot.Email.Abstractions;
using XiHan.Framework.Bot.Email.Models;
using XiHan.Framework.Bot.Email.Options;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// SaaS 邮件配置存储（数据库实现，覆盖框架默认 Options 实现）
/// </summary>
/// <remarks>
/// 每次读取「默认且启用」的 <c>SysEmailConfig</c>（含租户过滤）并解密认证密码，
/// 映射为框架自有模型 <see cref="EmailOptions"/>（FromPassword 为已解密明文）。
/// 无默认行返回 null（提供者按未配置处理）。
/// Singleton 生命周期，Scoped 仓储经 <see cref="IServiceScopeFactory"/> 开作用域解析。
/// </remarks>
public sealed class SaasEmailConfigStore : IEmailConfigStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailConfigSecretProtector _secretProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂（用于解析 Scoped 仓储）</param>
    /// <param name="secretProtector">邮件网关认证密码保护器</param>
    public SaasEmailConfigStore(
        IServiceScopeFactory scopeFactory,
        IEmailConfigSecretProtector secretProtector)
    {
        _scopeFactory = scopeFactory;
        _secretProtector = secretProtector;
    }

    /// <inheritdoc />
    public async Task<EmailOptions?> GetAsync(CancellationToken cancellationToken = default)
    {
        SysEmailConfig? config;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IEmailConfigRepository>();
            config = await repository.GetDefaultAsync(cancellationToken);
        }

        if (config is null)
        {
            return null;
        }

        return new EmailOptions
        {
            Enabled = true,
            From = new EmailFromModel
            {
                SmtpHost = config.SmtpHost,
                SmtpPort = config.SmtpPort,
                UseSsl = config.UseSsl,
                FromMail = config.FromEmail,
                FromName = config.FromName,
                FromUserName = config.UserName ?? string.Empty,
                FromPassword = _secretProtector.Unprotect(config.Password) ?? string.Empty,
                AcceptInvalidCertificate = config.AcceptInvalidCertificate
            },
            IsBodyHtml = config.IsBodyHtml
        };
    }
}
