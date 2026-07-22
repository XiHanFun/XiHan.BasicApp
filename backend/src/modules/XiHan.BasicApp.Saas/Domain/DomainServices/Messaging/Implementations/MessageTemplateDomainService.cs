// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 消息模板领域服务实现
/// </summary>
public sealed class MessageTemplateDomainService
    : IMessageTemplateDomainService
{
    private readonly IMessageTemplateRepository _messageTemplateRepository;

    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageTemplateDomainService(
        IMessageTemplateRepository messageTemplateRepository,
        ICurrentTenant currentTenant)
    {
        _messageTemplateRepository = messageTemplateRepository;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task<MessageTemplateCommandResult> CreateAsync(MessageTemplateCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCommon(command.TemplateName, command.Subject, command.Content, command.Description, command.Remark);
        ValidateEnum(command.Channel, nameof(command.Channel));
        ValidateEnum(command.Status, nameof(command.Status));

        var templateCode = NormalizeCode(command.TemplateCode);
        // 同租户同渠道内编码唯一（全局过滤合并 当前租户+全局，租户可建同编码覆盖全局，故只查当前上下文租户维度）
        var scopeTenantId = _currentTenant.Id ?? 0;
        if (await _messageTemplateRepository.AnyAsync(
                template => template.TenantId == scopeTenantId
                    && template.Channel == command.Channel
                    && template.TemplateCode == templateCode,
                cancellationToken))
        {
            throw new InvalidOperationException("同渠道下模板编码已存在。");
        }

        var template = new SysMessageTemplate
        {
            TemplateCode = templateCode,
            Channel = command.Channel,
            TemplateName = command.TemplateName.Trim(),
            Subject = NormalizeNullable(command.Subject),
            Content = command.Content,
            IsHtml = command.IsHtml,
            Description = NormalizeNullable(command.Description),
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        var saved = await _messageTemplateRepository.AddAsync(template, cancellationToken);
        return new MessageTemplateCommandResult(saved);
    }

    /// <inheritdoc />
    public async Task<MessageTemplateCommandResult> UpdateAsync(MessageTemplateUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCommon(command.TemplateName, command.Subject, command.Content, command.Description, command.Remark);

        var template = await GetEditableOrThrowAsync(command.BasicId, cancellationToken);
        template.TemplateName = command.TemplateName.Trim();
        template.Subject = NormalizeNullable(command.Subject);
        template.Content = command.Content;
        template.IsHtml = command.IsHtml;
        template.Description = NormalizeNullable(command.Description);
        template.Sort = command.Sort;
        template.Remark = NormalizeNullable(command.Remark);

        var saved = await _messageTemplateRepository.UpdateAsync(template, cancellationToken);
        return new MessageTemplateCommandResult(saved);
    }

    /// <inheritdoc />
    public async Task<MessageTemplateCommandResult> UpdateStatusAsync(MessageTemplateStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEnum(command.Status, nameof(command.Status));

        var template = await GetEditableOrThrowAsync(command.BasicId, cancellationToken);
        template.Status = command.Status;
        template.Remark = NormalizeNullable(command.Remark) ?? template.Remark;

        var saved = await _messageTemplateRepository.UpdateAsync(template, cancellationToken);
        return new MessageTemplateCommandResult(saved);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var template = await GetEditableOrThrowAsync(id, cancellationToken);
        if (!await _messageTemplateRepository.DeleteAsync(template, cancellationToken))
        {
            throw new InvalidOperationException("消息模板删除失败。");
        }
    }

    private static string NormalizeCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalized = code.Trim();
        if (normalized.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(code), "模板编码不能超过 100 个字符。");
        }

        if (normalized.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("模板编码不能包含空白字符。");
        }

        return normalized;
    }

    private static void ValidateCommon(string templateName, string? subject, string content, string? description, string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (templateName.Trim().Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(templateName), "模板名称不能超过 100 个字符。");
        }

        if (!string.IsNullOrWhiteSpace(subject) && subject.Trim().Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(subject), "主题模板不能超过 200 个字符。");
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(description), "模板描述不能超过 500 个字符。");
        }

        if (!string.IsNullOrWhiteSpace(remark) && remark.Trim().Length > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(remark), "备注不能超过 500 个字符。");
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
            where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 获取可维护的模板：全局模板（TenantId=0）仅平台运维态可维护（与菜单/权限等全局模板同一不变量）
    /// </summary>
    private async Task<SysMessageTemplate> GetEditableOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "模板主键必须大于 0。");
        }

        var template = await _messageTemplateRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("消息模板不存在。");

        if (template.IsGlobal && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台级全局模板仅平台运维态可维护，请切换到平台运维后操作。");
        }

        return template;
    }
}
