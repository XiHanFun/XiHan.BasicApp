// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Configurations;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Chat.Application.Services;

/// <summary>
/// 聊天敏感词守卫：命中即 fail-closed 拒绝发送/编辑
/// </summary>
public interface IChatSensitiveWordGuard
{
    /// <summary>
    /// 校验文本内容不含敏感词（命中抛出业务异常）
    /// </summary>
    Task EnsureAllowedAsync(string? content, CancellationToken cancellationToken = default);
}

/// <summary>
/// 聊天敏感词守卫实现
/// </summary>
/// <remarks>
/// 词库是聊天策略（<see cref="ChatConfigKeys.Policy"/>）里的敏感词数组，租户有同键配置时用租户的；
/// 配置值查询自带缓存且在修改配置时失效。不区分大小写的包含匹配，命中即拒绝。
/// </remarks>
public sealed class ChatSensitiveWordGuard : IChatSensitiveWordGuard, IScopedDependency
{
    private readonly ISaasConfigurationService _configuration;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatSensitiveWordGuard(ISaasConfigurationService configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 校验文本内容不含敏感词（命中抛出业务异常）
    /// </summary>
    public async Task EnsureAllowedAsync(string? content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var policy = await _configuration.GetJsonAsync(ChatConfigKeys.Policy, new ChatPolicySettings(), cancellationToken);
        foreach (var word in policy.SensitiveWords.Where(static word => !string.IsNullOrWhiteSpace(word)))
        {
            if (content.Contains(word.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("消息包含敏感词，已被拦截。");
            }
        }
    }
}
