// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成模板领域服务实现
/// </summary>
public sealed class CodeGenTemplateDomainService
    : ICodeGenTemplateDomainService
{
    private readonly ICodeGenTemplateRepository _templateRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTemplateDomainService(ICodeGenTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    /// <inheritdoc />
    public async Task<CodeGenTemplateCommandResult> CreateTemplateAsync(CodeGenTemplateCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        // 模板类型可为空（通用模板，适用于全部类型），仅在显式指定时校验取值合法
        if (command.TemplateType is { } templateType)
        {
            EnsureEnum(templateType, nameof(command.TemplateType));
        }

        EnsureEnum(command.TemplateEngine, nameof(command.TemplateEngine));
        EnsureEnum(command.Status, nameof(command.Status));

        var templateCode = Required(command.TemplateCode, 100, nameof(command.TemplateCode), "模板编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(templateCode, "模板编码不能包含空白字符。");
        if (await _templateRepository.ExistsCodeAsync(templateCode, null, cancellationToken))
        {
            throw new InvalidOperationException("模板编码已存在。");
        }

        var template = new SysCodeGenTemplate
        {
            TemplateCode = templateCode,
            TemplateName = Required(command.TemplateName, 200, nameof(command.TemplateName), "模板名称不能超过 200 个字符。"),
            TemplateDescription = Optional(command.TemplateDescription, 500, nameof(command.TemplateDescription), "模板描述不能超过 500 个字符。"),
            TemplateGroup = Optional(command.TemplateGroup, 100, nameof(command.TemplateGroup), "模板分组不能超过 100 个字符。"),
            TemplateType = command.TemplateType,
            TemplateEngine = command.TemplateEngine,
            WriteMode = command.WriteMode,
            TemplateContent = NormalizeNullable(command.TemplateContent),
            FileNameExpression = Optional(command.FileNameExpression, 500, nameof(command.FileNameExpression), "生成文件名表达式不能超过 500 个字符。"),
            FilePathExpression = Optional(command.FilePathExpression, 500, nameof(command.FilePathExpression), "生成文件路径表达式不能超过 500 个字符。"),
            FileExtension = Optional(command.FileExtension, 20, nameof(command.FileExtension), "文件扩展名不能超过 20 个字符。"),
            IsBuiltIn = false,
            IsEnabled = true,
            Sort = command.Sort,
            Status = command.Status,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new CodeGenTemplateCommandResult(await _templateRepository.AddAsync(template, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<CodeGenTemplateCommandResult> UpdateTemplateAsync(CodeGenTemplateUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "模板主键必须大于 0。");
        // 模板类型可为空（通用模板，适用于全部类型），仅在显式指定时校验取值合法
        if (command.TemplateType is { } templateType)
        {
            EnsureEnum(templateType, nameof(command.TemplateType));
        }

        EnsureEnum(command.TemplateEngine, nameof(command.TemplateEngine));

        var template = await GetTemplateOrThrowAsync(command.BasicId, cancellationToken);

        // 模板编码不可变（内置或非内置均不允许改编码），故此处不更新 TemplateCode。
        // 内置模板允许修改内容、名称等业务字段。
        template.TemplateName = Required(command.TemplateName, 200, nameof(command.TemplateName), "模板名称不能超过 200 个字符。");
        template.TemplateDescription = Optional(command.TemplateDescription, 500, nameof(command.TemplateDescription), "模板描述不能超过 500 个字符。");
        template.TemplateGroup = Optional(command.TemplateGroup, 100, nameof(command.TemplateGroup), "模板分组不能超过 100 个字符。");
        template.TemplateType = command.TemplateType;
        template.TemplateEngine = command.TemplateEngine;
        template.WriteMode = command.WriteMode;
        template.TemplateContent = NormalizeNullable(command.TemplateContent);
        template.FileNameExpression = Optional(command.FileNameExpression, 500, nameof(command.FileNameExpression), "生成文件名表达式不能超过 500 个字符。");
        template.FilePathExpression = Optional(command.FilePathExpression, 500, nameof(command.FilePathExpression), "生成文件路径表达式不能超过 500 个字符。");
        template.FileExtension = Optional(command.FileExtension, 20, nameof(command.FileExtension), "文件扩展名不能超过 20 个字符。");
        template.IsEnabled = command.IsEnabled;
        template.Sort = command.Sort;
        template.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new CodeGenTemplateCommandResult(await _templateRepository.UpdateAsync(template, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<CodeGenTemplateCommandResult> UpdateTemplateStatusAsync(CodeGenTemplateStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "模板主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var template = await GetTemplateOrThrowAsync(command.BasicId, cancellationToken);
        template.Status = command.Status;
        template.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? template.Remark;

        return new CodeGenTemplateCommandResult(await _templateRepository.UpdateAsync(template, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DeleteTemplateAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var template = await GetTemplateOrThrowAsync(id, cancellationToken);
        if (template.IsBuiltIn)
        {
            throw new InvalidOperationException("内置模板不能删除。");
        }

        if (!await _templateRepository.DeleteAsync(template, cancellationToken))
        {
            throw new InvalidOperationException("模板删除失败。");
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

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

    private async Task<SysCodeGenTemplate> GetTemplateOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "模板主键必须大于 0。");
        return await _templateRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("模板不存在。");
    }
}
