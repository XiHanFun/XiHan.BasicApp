// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Entities;

namespace XiHan.BasicApp.AI.Application.Mappers;

/// <summary>
/// AI 提示词应用层映射器（手写静态映射）
/// </summary>
public static class AiPromptApplicationMapper
{
    /// <summary>
    /// 映射创建命令
    /// </summary>
    public static AiPromptCreateCommand ToCreateCommand(AiPromptCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new AiPromptCreateCommand(
            input.PromptCode,
            input.PromptName,
            input.Category,
            input.Version,
            input.Content,
            input.IsEnabled,
            input.Sort,
            input.Status,
            input.Remark);
    }

    /// <summary>
    /// 映射更新命令
    /// </summary>
    public static AiPromptUpdateCommand ToUpdateCommand(AiPromptUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new AiPromptUpdateCommand(
            input.BasicId,
            input.PromptName,
            input.Category,
            input.Version,
            input.Content,
            input.IsEnabled,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射状态命令
    /// </summary>
    public static AiPromptStatusChangeCommand ToStatusCommand(AiPromptStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new AiPromptStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 实体 → 列表项 DTO
    /// </summary>
    public static AiPromptListItemDto ToListItemDto(SysAiPrompt entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AiPromptListItemDto
        {
            BasicId = entity.BasicId,
            PromptCode = entity.PromptCode,
            PromptName = entity.PromptName,
            Category = entity.Category,
            Version = entity.Version,
            IsEnabled = entity.IsEnabled,
            Sort = entity.Sort,
            Status = entity.Status,
            CreatedTime = entity.CreatedTime,
            ModifiedTime = entity.ModifiedTime
        };
    }

    /// <summary>
    /// 实体 → 详情 DTO（含正文）
    /// </summary>
    public static AiPromptDetailDto ToDetailDto(SysAiPrompt entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var item = ToListItemDto(entity);
        return new AiPromptDetailDto
        {
            BasicId = item.BasicId,
            PromptCode = item.PromptCode,
            PromptName = item.PromptName,
            Category = item.Category,
            Version = item.Version,
            IsEnabled = item.IsEnabled,
            Sort = item.Sort,
            Status = item.Status,
            CreatedTime = item.CreatedTime,
            ModifiedTime = item.ModifiedTime,
            Content = entity.Content,
            Remark = entity.Remark,
            CreatedId = entity.CreatedId,
            CreatedBy = entity.CreatedBy,
            ModifiedId = entity.ModifiedId,
            ModifiedBy = entity.ModifiedBy
        };
    }
}
