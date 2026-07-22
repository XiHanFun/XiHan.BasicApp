// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 消息模板应用层映射器
/// </summary>
public static class MessageTemplateApplicationMapper
{
    /// <summary>
    /// 映射创建命令
    /// </summary>
    public static MessageTemplateCreateCommand ToCreateCommand(MessageTemplateCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MessageTemplateCreateCommand(
            input.TemplateCode,
            input.Channel,
            input.TemplateName,
            input.Subject,
            input.Content,
            input.IsHtml,
            input.Description,
            input.Status,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射更新命令
    /// </summary>
    public static MessageTemplateUpdateCommand ToUpdateCommand(MessageTemplateUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MessageTemplateUpdateCommand(
            input.BasicId,
            input.TemplateName,
            input.Subject,
            input.Content,
            input.IsHtml,
            input.Description,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射状态命令
    /// </summary>
    public static MessageTemplateStatusChangeCommand ToStatusCommand(MessageTemplateStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MessageTemplateStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射列表项
    /// </summary>
    public static MessageTemplateListItemDto ToListItemDto(SysMessageTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return new MessageTemplateListItemDto
        {
            BasicId = template.BasicId,
            TemplateCode = template.TemplateCode,
            Channel = template.Channel,
            TemplateName = template.TemplateName,
            Subject = template.Subject,
            IsHtml = template.IsHtml,
            IsGlobal = template.IsGlobal,
            Description = template.Description,
            Status = template.Status,
            Sort = template.Sort,
            Remark = template.Remark
        };
    }

    /// <summary>
    /// 映射详情
    /// </summary>
    public static MessageTemplateDetailDto ToDetailDto(SysMessageTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return new MessageTemplateDetailDto
        {
            BasicId = template.BasicId,
            TemplateCode = template.TemplateCode,
            Channel = template.Channel,
            TemplateName = template.TemplateName,
            Subject = template.Subject,
            Content = template.Content,
            IsHtml = template.IsHtml,
            IsGlobal = template.IsGlobal,
            Description = template.Description,
            Status = template.Status,
            Sort = template.Sort,
            Remark = template.Remark
        };
    }
}
