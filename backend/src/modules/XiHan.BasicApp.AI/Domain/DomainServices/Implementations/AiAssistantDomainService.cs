// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;

namespace XiHan.BasicApp.AI.Domain.DomainServices.Implementations;

/// <summary>
/// AI 助手领域服务实现
/// </summary>
public sealed class AiAssistantDomainService : IAiAssistantDomainService
{
    /// <summary>
    /// 检索片段数上限（超过后提示词会挤占模型上下文）
    /// </summary>
    private const int MaxKnowledgeTopK = 20;

    /// <summary>
    /// 带入历史消息条数上限
    /// </summary>
    private const int MaxHistoryRounds = 50;

    private readonly IAiAssistantRepository _assistantRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiAssistantDomainService(IAiAssistantRepository assistantRepository)
    {
        _assistantRepository = assistantRepository;
    }

    /// <inheritdoc />
    public async Task<AiAssistantCommandResult> CreateAssistantAsync(AiAssistantCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureEnum(command.Status, nameof(command.Status));

        var assistantCode = Required(command.AssistantCode, 100, nameof(command.AssistantCode), "助手编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(assistantCode, "助手编码不能包含空白字符。");
        if (await _assistantRepository.ExistsCodeAsync(assistantCode, null, cancellationToken))
        {
            throw new InvalidOperationException("助手编码已存在。");
        }

        var assistant = new SysAiAssistant
        {
            AssistantCode = assistantCode,
            AssistantName = Required(command.AssistantName, 100, nameof(command.AssistantName), "助手名称不能超过 100 个字符。"),
            Avatar = Optional(command.Avatar, 500, nameof(command.Avatar), "助手头像不能超过 500 个字符。"),
            Description = Optional(command.Description, 500, nameof(command.Description), "助手简介不能超过 500 个字符。"),
            Greeting = Optional(command.Greeting, 1000, nameof(command.Greeting), "开场白不能超过 1000 个字符。"),
            PromptCode = Optional(command.PromptCode, 100, nameof(command.PromptCode), "提示词编码不能超过 100 个字符。"),
            ProviderCode = Optional(command.ProviderCode, 100, nameof(command.ProviderCode), "provider 编码不能超过 100 个字符。"),
            EnableKnowledge = command.EnableKnowledge,
            KnowledgeProviderCode = Optional(command.KnowledgeProviderCode, 100, nameof(command.KnowledgeProviderCode), "嵌入 provider 编码不能超过 100 个字符。"),
            KnowledgeTopK = EnsureRange(command.KnowledgeTopK, 1, MaxKnowledgeTopK, nameof(command.KnowledgeTopK), $"检索片段数须在 1~{MaxKnowledgeTopK} 之间。"),
            HistoryRounds = EnsureRange(command.HistoryRounds, 0, MaxHistoryRounds, nameof(command.HistoryRounds), $"带入历史消息条数须在 0~{MaxHistoryRounds} 之间。"),
            IsDefault = command.IsDefault,
            IsEnabled = command.IsEnabled,
            Sort = command.Sort,
            Status = command.Status,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        var created = await _assistantRepository.AddAsync(assistant, cancellationToken);
        if (created.IsDefault)
        {
            await ClearOtherDefaultsAsync(created.BasicId, cancellationToken);
        }

        return new AiAssistantCommandResult(created);
    }

    /// <inheritdoc />
    public async Task<AiAssistantCommandResult> UpdateAssistantAsync(AiAssistantUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "助手主键必须大于 0。");
        var assistant = await GetAssistantOrThrowAsync(command.BasicId, cancellationToken);

        // 助手编码不可变：会话按助手主键绑定，改编码不影响既有会话，但会让配置与人读标识脱节。
        assistant.AssistantName = Required(command.AssistantName, 100, nameof(command.AssistantName), "助手名称不能超过 100 个字符。");
        assistant.Avatar = Optional(command.Avatar, 500, nameof(command.Avatar), "助手头像不能超过 500 个字符。");
        assistant.Description = Optional(command.Description, 500, nameof(command.Description), "助手简介不能超过 500 个字符。");
        assistant.Greeting = Optional(command.Greeting, 1000, nameof(command.Greeting), "开场白不能超过 1000 个字符。");
        assistant.PromptCode = Optional(command.PromptCode, 100, nameof(command.PromptCode), "提示词编码不能超过 100 个字符。");
        assistant.ProviderCode = Optional(command.ProviderCode, 100, nameof(command.ProviderCode), "provider 编码不能超过 100 个字符。");
        assistant.EnableKnowledge = command.EnableKnowledge;
        assistant.KnowledgeProviderCode = Optional(command.KnowledgeProviderCode, 100, nameof(command.KnowledgeProviderCode), "嵌入 provider 编码不能超过 100 个字符。");
        assistant.KnowledgeTopK = EnsureRange(command.KnowledgeTopK, 1, MaxKnowledgeTopK, nameof(command.KnowledgeTopK), $"检索片段数须在 1~{MaxKnowledgeTopK} 之间。");
        assistant.HistoryRounds = EnsureRange(command.HistoryRounds, 0, MaxHistoryRounds, nameof(command.HistoryRounds), $"带入历史消息条数须在 0~{MaxHistoryRounds} 之间。");
        assistant.IsDefault = command.IsDefault;
        assistant.IsEnabled = command.IsEnabled;
        assistant.Sort = command.Sort;
        assistant.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        var updated = await _assistantRepository.UpdateAsync(assistant, cancellationToken);
        if (updated.IsDefault)
        {
            await ClearOtherDefaultsAsync(updated.BasicId, cancellationToken);
        }

        return new AiAssistantCommandResult(updated);
    }

    /// <inheritdoc />
    public async Task<AiAssistantCommandResult> UpdateAssistantStatusAsync(AiAssistantStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "助手主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var assistant = await GetAssistantOrThrowAsync(command.BasicId, cancellationToken);
        assistant.Status = command.Status;
        assistant.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? assistant.Remark;

        return new AiAssistantCommandResult(await _assistantRepository.UpdateAsync(assistant, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AiAssistantCommandResult> SetDefaultAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var assistant = await GetAssistantOrThrowAsync(id, cancellationToken);
        if (!assistant.IsEnabled)
        {
            throw new InvalidOperationException("已禁用的助手不能设为默认。");
        }

        await ClearOtherDefaultsAsync(assistant.BasicId, cancellationToken);
        if (!assistant.IsDefault)
        {
            assistant.IsDefault = true;
            assistant = await _assistantRepository.UpdateAsync(assistant, cancellationToken);
        }

        return new AiAssistantCommandResult(assistant);
    }

    /// <inheritdoc />
    public async Task DeleteAssistantAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var assistant = await GetAssistantOrThrowAsync(id, cancellationToken);
        if (!await _assistantRepository.DeleteAsync(assistant, cancellationToken))
        {
            throw new InvalidOperationException("助手删除失败。");
        }
    }

    /// <summary>
    /// 清除除 keepId 外其它行的默认标记（单默认互斥）
    /// </summary>
    private async Task ClearOtherDefaultsAsync(long keepId, CancellationToken cancellationToken)
    {
        var others = await _assistantRepository.GetOtherDefaultsAsync(keepId, cancellationToken);
        foreach (var other in others)
        {
            other.IsDefault = false;
            _ = await _assistantRepository.UpdateAsync(other, cancellationToken);
        }
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static int EnsureRange(int value, int min, int max, string paramName, string message)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return value;
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private async Task<SysAiAssistant> GetAssistantOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "助手主键必须大于 0。");
        return await _assistantRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("助手不存在。");
    }
}
