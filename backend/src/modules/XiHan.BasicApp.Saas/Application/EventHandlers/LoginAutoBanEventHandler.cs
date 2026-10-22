// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 登录失败计数：按客户端 IP 累计，达到阈值即自动封禁
/// </summary>
/// <remarks>
/// 访问策略拒绝的登录（凭据是对的）不计数；计数出错只记日志，不影响登录失败本身的处理。
/// </remarks>
public sealed class LoginAutoBanEventHandler : ILocalEventHandler<AuthLoginFailedDomainEvent>
{
    private readonly ILoginBanService _banService;
    private readonly ILogger<LoginAutoBanEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LoginAutoBanEventHandler(ILoginBanService banService, ILogger<LoginAutoBanEventHandler> logger)
    {
        _banService = banService;
        _logger = logger;
    }

    /// <summary>
    /// 事件处理器通过实现此方法来处理事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(AuthLoginFailedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.LoginResult == LoginResult.AccessDenied || string.IsNullOrWhiteSpace(eventData.IpAddress))
        {
            return;
        }

        try
        {
            await _banService.RecordFailureAsync(eventData.IpAddress.Trim());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "登录失败计数出错，IP {Ip}", eventData.IpAddress);
        }
    }
}
