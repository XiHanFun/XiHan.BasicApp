// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 模板过滤器在渲染时发现的配置问题。
/// </summary>
/// <remarks>
/// 带消息键与参数，引擎按调用方区域出文案（4.x 是写死的中文）。
/// </remarks>
public sealed class CodeGenRenderException : Exception
{
    /// <summary>
    /// 构造。
    /// </summary>
    public CodeGenRenderException()
    {
        MessageKey = string.Empty;
        Arguments = [];
    }

    /// <summary>
    /// 构造。
    /// </summary>
    /// <param name="message">日志用的说明。</param>
    public CodeGenRenderException(string message)
        : base(message)
    {
        MessageKey = string.Empty;
        Arguments = [];
    }

    /// <summary>
    /// 构造。
    /// </summary>
    /// <param name="message">日志用的说明。</param>
    /// <param name="innerException">内层异常。</param>
    public CodeGenRenderException(string message, Exception innerException)
        : base(message, innerException)
    {
        MessageKey = string.Empty;
        Arguments = [];
    }

    /// <summary>
    /// 构造。
    /// </summary>
    /// <param name="messageKey">消息键。</param>
    /// <param name="arguments">消息参数。</param>
    public CodeGenRenderException(string messageKey, params (string Name, object? Value)[] arguments)
        : base(messageKey)
    {
        MessageKey = messageKey;
        Arguments = arguments;
    }

    /// <summary>
    /// 消息键。
    /// </summary>
    public string MessageKey { get; }

    /// <summary>
    /// 消息参数。
    /// </summary>
    public IReadOnlyList<(string Name, object? Value)> Arguments { get; }
}
