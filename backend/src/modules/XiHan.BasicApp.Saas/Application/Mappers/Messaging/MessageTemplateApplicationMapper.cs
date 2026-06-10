#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageTemplateApplicationMapper
// Guid:6c7be300-479c-4dfe-db50-c3eba6b77deb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
