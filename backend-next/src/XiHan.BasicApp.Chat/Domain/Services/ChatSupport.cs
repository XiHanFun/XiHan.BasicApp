// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.Json;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Operations.ChatMessages;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Domain.Services;

/// <summary>
/// 发起调用的用户与所在的一侧
/// </summary>
/// <param name="UserId">用户</param>
/// <param name="Side">侧（租户 ID，平台为 <c>platform</c>）</param>
public sealed record ChatCaller(Guid UserId, string Side)
{
    /// <summary>
    /// 取调用方
    /// </summary>
    /// <param name="context">调用上下文</param>
    public static Result<ChatCaller> Of(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var principal = context.GetSecurity().Principal;
        return Guid.TryParse(principal.Id, out var userId)
            ? new ChatCaller(userId, SideRows.SideOf(context))
            : Error.NotFound(ChatErrors.UserNotFound, ChatMessages.UserNotFound, MessageArguments.Of(("id", principal.Id)));
    }
}

/// <summary>
/// 聊天的规则常量
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>ChatDomainService</c>
/// </remarks>
public static class ChatRules
{
    /// <summary>
    /// 撤回时限（分钟）
    /// </summary>
    public const int RecallWindowMinutes = 2;

    /// <summary>
    /// 编辑时限（分钟）
    /// </summary>
    public const int EditWindowMinutes = 5;

    /// <summary>
    /// 文本正文的长度上限
    /// </summary>
    public const int MaxContentLength = 4000;

    /// <summary>
    /// 助手回复的长度上限
    /// </summary>
    /// <remarks>
    /// 超长截断（照 4.x）
    /// </remarks>
    public const int MaxAssistantContentLength = 20000;

    /// <summary>
    /// 会话预览的长度上限
    /// </summary>
    public const int MaxPreviewLength = 200;

    /// <summary>
    /// 回复快照的长度上限
    /// </summary>
    public const int MaxReplyPreviewLength = 300;

    /// <summary>
    /// 每会话置顶消息的上限
    /// </summary>
    public const int MaxPinnedMessages = 10;

    /// <summary>
    /// 一条消息提及人数的上限
    /// </summary>
    public const int MaxMentionCount = 20;

    /// <summary>
    /// 语音时长的上限（秒）
    /// </summary>
    public const int MaxVoiceSeconds = 60;

    /// <summary>
    /// 系统提示里直接列出的名字上限
    /// </summary>
    public const int MaxNamesInSystemMessage = 3;

    /// <summary>
    /// 聊天附件在文件里的来源
    /// </summary>
    public const string AttachmentSource = "chat";

    /// <summary>
    /// 助手会话的唯一键：一个用户对一个助手只有一个会话
    /// </summary>
    /// <remarks>
    /// 照 4.x
    /// </remarks>
    public static string AssistantPairKeyOf(Guid assistantId, Guid userId) => $"ai:{assistantId:N}:{userId:N}";

    /// <summary>
    /// 单聊的唯一键（两个用户 ID 小者在前）
    /// </summary>
    public static string PairKeyOf(Guid userId, Guid peerUserId)
    {
        var (first, second) = string.CompareOrdinal(userId.ToString("N"), peerUserId.ToString("N")) <= 0 ? (userId, peerUserId) : (peerUserId, userId);
        return $"{first:N}_{second:N}";
    }

    /// <summary>
    /// 截断到上限
    /// </summary>
    public static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>
    /// 可选文本：空白为空，超长截断
    /// </summary>
    public static string? Optional(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : Truncate(value.Trim(), maxLength);
}

/// <summary>
/// 写进库里的系统提示与会话预览文案（作用域）
/// </summary>
/// <remarks>
/// 按发起调用的区域取消息目录里的文案（4.x 写死中文）；键都在 10 种区域里，取不到是缺陷，直接抛出
/// </remarks>
/// <param name="localizer">消息本地化</param>
public sealed class ChatTexts(IMessageLocalizer localizer)
{
    /// <summary>
    /// 取一条文案
    /// </summary>
    public string Render(InvocationContext context, string key, params (string Name, object? Value)[] arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        var locale = localizer.Options.Resolve(context);
        var values = arguments.ToDictionary(static a => a.Name, static a => a.Value, StringComparer.Ordinal);
        return localizer.Resolve(key, locale, values).TryGetValue(out var text)
            ? text.Text
            : throw new InvalidOperationException($"Chat text '{key}' is missing for locale '{locale}'.");
    }

    /// <summary>
    /// 系统提示里的一串名字（超过三个折叠为「等 N 人」）
    /// </summary>
    public string Names(InvocationContext context, IReadOnlyList<string?> names)
    {
        var valid = names.Where(static n => !string.IsNullOrWhiteSpace(n)).Select(static n => n!).ToList();
        if (valid.Count == 0)
        {
            return Render(context, ChatMessages.SystemNamesCount, ("count", names.Count));
        }

        var separator = Render(context, ChatMessages.SystemNamesSeparator);
        return valid.Count <= ChatRules.MaxNamesInSystemMessage
            ? string.Join(separator, valid)
            : Render(context, ChatMessages.SystemNamesMore, ("names", string.Join(separator, valid.Take(ChatRules.MaxNamesInSystemMessage))), ("count", valid.Count));
    }

    /// <summary>
    /// 会话里最后一条消息的预览
    /// </summary>
    public string Preview(InvocationContext context, ChatMessageType type, string? content, IReadOnlyList<ChatMessageAttachment> attachments)
    {
        var preview = type switch
        {
            ChatMessageType.Image => attachments.Count > 1 ? Render(context, ChatMessages.PreviewImages, ("count", attachments.Count)) : Render(context, ChatMessages.PreviewImage),
            ChatMessageType.Voice => Render(context, ChatMessages.PreviewVoice, ("seconds", attachments.Count > 0 ? attachments[0].DurationSeconds ?? 0 : 0)),
            ChatMessageType.File => attachments.Count > 1 ? Render(context, ChatMessages.PreviewFiles, ("count", attachments.Count)) : Render(context, ChatMessages.PreviewFile, ("name", attachments.Count > 0 ? attachments[0].FileName : string.Empty)),
            _ => content ?? string.Empty,
        };
        return ChatRules.Truncate(preview.Trim(), ChatRules.MaxPreviewLength);
    }
}

/// <summary>
/// 聊天策略参数 <c>chat.policy</c> 的值
/// </summary>
/// <remarks>
/// 照 4.x：保留天数只读平台值，敏感词租户可以覆盖（读参数时当前租户的同键参数优先）
/// </remarks>
public sealed record ChatPolicySettings
{
    /// <summary>
    /// 消息保留天数
    /// </summary>
    public int RetentionDays { get; set; } = ChatPolicy.DefaultRetentionDays;

    /// <summary>
    /// 敏感词（不区分大小写的包含匹配）
    /// </summary>
    public IReadOnlyList<string> SensitiveWords { get; set; } = [];
}

/// <summary>
/// 聊天策略（作用域）
/// </summary>
/// <remarks>
/// 参数 <c>chat.policy</c>（JSON，<c>{"retentionDays":365,"sensitiveWords":[]}</c>），没建即用缺省值；建了而 JSON 不合法或保留天数不在 1 到 3650 之间即失败（不按错误的值删数据或放过敏感词）
/// 敏感词照 4.x 管发送与编辑的正文（含图片与文件的说明），不管群名、公告与文件名
/// </remarks>
/// <param name="readers">参数读取（Settings 实现）</param>
public sealed class ChatPolicy(IEnumerable<IConfigValueReader> readers)
{
    /// <summary>
    /// 参数键
    /// </summary>
    public const string ConfigKey = "chat.policy";

    /// <summary>
    /// 缺省保留天数
    /// </summary>
    public const int DefaultRetentionDays = 365;

    /// <summary>
    /// 保留天数的上限（十年）
    /// </summary>
    public const int MaxRetentionDays = 3650;

    /// <summary>
    /// 读当前上下文的策略
    /// </summary>
    public async ValueTask<Result<ChatPolicySettings>> ReadAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (readers.FirstOrDefault() is not { } reader)
        {
            return new ChatPolicySettings();
        }

        var configured = await reader.GetAsync(ConfigKey, context).ConfigureAwait(false);
        if (!configured.TryGetValue(out var value))
        {
            return configured.Error!;
        }

        if (!value.TryGetValue(out var json))
        {
            return new ChatPolicySettings();
        }

        ChatPolicySettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize(json, ChatJsonContext.Default.ChatPolicySettings);
        }
        catch (JsonException)
        {
            settings = null;
        }

        return settings is { RetentionDays: >= 1 and <= MaxRetentionDays }
            ? settings
            : Error.Validation(ChatErrors.PolicyInvalid, ChatMessages.PolicyInvalid, MessageArguments.Of(("key", ConfigKey)));
    }

    /// <summary>
    /// 核对正文不含敏感词
    /// </summary>
    public async ValueTask<Result> EnsureAllowedAsync(InvocationContext context, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Success();
        }

        var policy = await ReadAsync(context).ConfigureAwait(false);
        if (!policy.TryGetValue(out var settings))
        {
            return policy.Error!;
        }

        return settings.SensitiveWords.Where(static w => !string.IsNullOrWhiteSpace(w)).Any(w => content.Contains(w.Trim(), StringComparison.OrdinalIgnoreCase))
            ? Error.Validation(ChatErrors.SensitiveWord, ChatMessages.SensitiveWord)
            : Result.Success();
    }
}

/// <summary>
/// 消息附件的存取（JSON 数组）
/// </summary>
public static class ChatAttachmentsJson
{
    /// <summary>
    /// 写成 JSON
    /// </summary>
    /// <remarks>
    /// 没有附件为空
    /// </remarks>
    public static string? Serialize(IReadOnlyList<ChatMessageAttachment> attachments)
        => attachments.Count == 0 ? null : JsonSerializer.Serialize(attachments, ChatJsonContext.Default.IReadOnlyListChatMessageAttachment);

    /// <summary>
    /// 从 JSON 读出
    /// </summary>
    public static IReadOnlyList<ChatMessageAttachment> Deserialize(string? json)
        => string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize(json, ChatJsonContext.Default.IReadOnlyListChatMessageAttachment) ?? [];

    /// <summary>
    /// 提及的用户（逗号分隔）读出
    /// </summary>
    public static IReadOnlyList<Guid> MentionsOf(string? value)
        => string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(static v => Guid.Parse(v, CultureInfo.InvariantCulture)).ToList();

    /// <summary>
    /// 提及的用户写成逗号分隔
    /// </summary>
    public static string? MentionsText(IReadOnlyList<Guid> userIds) => userIds.Count == 0 ? null : string.Join(',', userIds.Select(static id => id.ToString("N")));
}
