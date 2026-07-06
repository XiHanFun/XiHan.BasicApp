#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiPromptDomainService
// Guid:4a7ea68c-7e53-44c6-bab8-788c7f0a2f76
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/06 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;

namespace XiHan.BasicApp.AI.Domain.DomainServices.Implementations;

/// <summary>
/// AI 提示词领域服务实现
/// </summary>
public sealed class AiPromptDomainService : IAiPromptDomainService
{
    private readonly IAiPromptRepository _promptRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiPromptDomainService(IAiPromptRepository promptRepository)
    {
        _promptRepository = promptRepository;
    }

    /// <inheritdoc />
    public async Task<AiPromptCommandResult> CreatePromptAsync(AiPromptCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureEnum(command.Status, nameof(command.Status));

        var promptCode = Required(command.PromptCode, 100, nameof(command.PromptCode), "提示词编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(promptCode, "提示词编码不能包含空白字符。");
        if (await _promptRepository.ExistsCodeAsync(promptCode, null, cancellationToken))
        {
            throw new InvalidOperationException("提示词编码已存在。");
        }

        var prompt = new SysAiPrompt
        {
            PromptCode = promptCode,
            PromptName = Required(command.PromptName, 200, nameof(command.PromptName), "提示词名称不能超过 200 个字符。"),
            Category = Optional(command.Category, 100, nameof(command.Category), "分类不能超过 100 个字符。"),
            Version = Optional(command.Version, 100, nameof(command.Version), "版本不能超过 100 个字符。"),
            Content = RequiredContent(command.Content),
            IsEnabled = command.IsEnabled,
            Sort = command.Sort,
            Status = command.Status,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new AiPromptCommandResult(await _promptRepository.AddAsync(prompt, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AiPromptCommandResult> UpdatePromptAsync(AiPromptUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "提示词主键必须大于 0。");
        var prompt = await GetPromptOrThrowAsync(command.BasicId, cancellationToken);

        // 编码不可变，故此处不更新 PromptCode。
        prompt.PromptName = Required(command.PromptName, 200, nameof(command.PromptName), "提示词名称不能超过 200 个字符。");
        prompt.Category = Optional(command.Category, 100, nameof(command.Category), "分类不能超过 100 个字符。");
        prompt.Version = Optional(command.Version, 100, nameof(command.Version), "版本不能超过 100 个字符。");
        prompt.Content = RequiredContent(command.Content);
        prompt.IsEnabled = command.IsEnabled;
        prompt.Sort = command.Sort;
        prompt.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new AiPromptCommandResult(await _promptRepository.UpdateAsync(prompt, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AiPromptCommandResult> UpdatePromptStatusAsync(AiPromptStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "提示词主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var prompt = await GetPromptOrThrowAsync(command.BasicId, cancellationToken);
        prompt.Status = command.Status;
        prompt.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? prompt.Remark;

        return new AiPromptCommandResult(await _promptRepository.UpdateAsync(prompt, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DeletePromptAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var prompt = await GetPromptOrThrowAsync(id, cancellationToken);
        if (!await _promptRepository.DeleteAsync(prompt, cancellationToken))
        {
            throw new InvalidOperationException("提示词删除失败。");
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

    private static string RequiredContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("提示词正文不能为空。");
        }

        return content;
    }

    private async Task<SysAiPrompt> GetPromptOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "提示词主键必须大于 0。");
        return await _promptRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("提示词不存在。");
    }
}
