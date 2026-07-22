// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 渲染结果
/// </summary>
/// <param name="Subject">渲染后的主题（模板未定义主题时为 null）</param>
/// <param name="Content">渲染后的内容</param>
/// <param name="IsHtml">内容是否 HTML</param>
public sealed record RenderedMessage(string? Subject, string Content, bool IsHtml);

/// <summary>
/// 消息模板渲染器
/// </summary>
/// <remarks>
/// 职责：按 渠道+编码 查找模板（缓存 → 仓储，租户模板优先回退全局）并用框架 Scriban 引擎渲染。
/// 模板不存在或已停用时返回 null，调用方自行回退（内置内容/原始内容），保证发送链路不因模板缺失中断。
/// </remarks>
public interface IMessageTemplateRenderer
{
    /// <summary>
    /// 渲染指定模板
    /// </summary>
    /// <param name="channel">消息渠道</param>
    /// <param name="templateCode">模板编码</param>
    /// <param name="variables">模板变量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>渲染结果；模板不存在/停用时为 null</returns>
    Task<RenderedMessage?> RenderAsync(
        MessageChannel channel,
        string templateCode,
        IDictionary<string, object?> variables,
        CancellationToken cancellationToken = default);
}
