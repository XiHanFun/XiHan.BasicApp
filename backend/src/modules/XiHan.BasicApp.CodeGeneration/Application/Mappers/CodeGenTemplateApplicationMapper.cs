// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Application.Mappers;

/// <summary>
/// 代码生成模板应用层映射器（手写静态映射，命令模式，对齐 Saas 约定）
/// </summary>
public static class CodeGenTemplateApplicationMapper
{
    /// <summary>
    /// 映射模板创建命令
    /// </summary>
    public static CodeGenTemplateCreateCommand ToCreateCommand(CodeGenTemplateCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CodeGenTemplateCreateCommand(
            input.TemplateCode,
            input.TemplateName,
            input.TemplateDescription,
            input.TemplateGroup,
            input.TemplateType,
            input.TemplateEngine,
            input.WriteMode,
            input.TemplateContent,
            input.FileNameExpression,
            input.FilePathExpression,
            input.FileExtension,
            input.Status,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射模板更新命令
    /// </summary>
    public static CodeGenTemplateUpdateCommand ToUpdateCommand(CodeGenTemplateUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CodeGenTemplateUpdateCommand(
            input.BasicId,
            input.TemplateName,
            input.TemplateDescription,
            input.TemplateGroup,
            input.TemplateType,
            input.TemplateEngine,
            input.WriteMode,
            input.TemplateContent,
            input.FileNameExpression,
            input.FilePathExpression,
            input.FileExtension,
            input.IsEnabled,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射模板状态命令
    /// </summary>
    public static CodeGenTemplateStatusChangeCommand ToStatusCommand(CodeGenTemplateStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CodeGenTemplateStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 实体 → 列表项 DTO
    /// </summary>
    public static CodeGenTemplateListItemDto ToListItemDto(SysCodeGenTemplate entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new CodeGenTemplateListItemDto
        {
            BasicId = entity.BasicId,
            TemplateCode = entity.TemplateCode,
            TemplateName = entity.TemplateName,
            TemplateDescription = entity.TemplateDescription,
            TemplateGroup = entity.TemplateGroup,
            TemplateType = entity.TemplateType,
            TemplateEngine = entity.TemplateEngine,
            WriteMode = entity.WriteMode,
            FileExtension = entity.FileExtension,
            IsBuiltIn = entity.IsBuiltIn,
            IsEnabled = entity.IsEnabled,
            Sort = entity.Sort,
            Status = entity.Status,
            CreatedTime = entity.CreatedTime,
            ModifiedTime = entity.ModifiedTime
        };
    }

    /// <summary>
    /// 实体 → 详情 DTO（含模板正文）
    /// </summary>
    public static CodeGenTemplateDetailDto ToDetailDto(SysCodeGenTemplate entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var item = ToListItemDto(entity);
        return new CodeGenTemplateDetailDto
        {
            BasicId = item.BasicId,
            TemplateCode = item.TemplateCode,
            TemplateName = item.TemplateName,
            TemplateDescription = item.TemplateDescription,
            TemplateGroup = item.TemplateGroup,
            TemplateType = item.TemplateType,
            TemplateEngine = item.TemplateEngine,
            WriteMode = item.WriteMode,
            FileExtension = item.FileExtension,
            IsBuiltIn = item.IsBuiltIn,
            IsEnabled = item.IsEnabled,
            Sort = item.Sort,
            Status = item.Status,
            CreatedTime = item.CreatedTime,
            ModifiedTime = item.ModifiedTime,
            TemplateContent = entity.TemplateContent,
            TemplatePath = entity.TemplatePath,
            FileNameExpression = entity.FileNameExpression,
            FilePathExpression = entity.FilePathExpression,
            Remark = entity.Remark,
            CreatedId = entity.CreatedId,
            CreatedBy = entity.CreatedBy,
            ModifiedId = entity.ModifiedId,
            ModifiedBy = entity.ModifiedBy
        };
    }
}
