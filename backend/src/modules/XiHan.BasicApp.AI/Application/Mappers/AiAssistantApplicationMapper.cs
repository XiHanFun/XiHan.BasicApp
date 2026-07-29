// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Entities;

namespace XiHan.BasicApp.AI.Application.Mappers;

/// <summary>
/// AI 助手应用层映射器（手写静态映射，命令模式，对齐 Saas 约定）
/// </summary>
public static class AiAssistantApplicationMapper
{
    /// <summary>
    /// 映射创建命令
    /// </summary>
    public static AiAssistantCreateCommand ToCreateCommand(AiAssistantCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new AiAssistantCreateCommand(
            input.AssistantCode,
            input.AssistantName,
            input.Avatar,
            input.Description,
            input.Greeting,
            input.PromptCode,
            input.ProviderCode,
            input.EnableKnowledge,
            input.KnowledgeProviderCode,
            input.KnowledgeTopK,
            input.HistoryRounds,
            input.IsDefault,
            input.IsEnabled,
            input.Sort,
            input.Status,
            input.Remark);
    }

    /// <summary>
    /// 映射更新命令
    /// </summary>
    public static AiAssistantUpdateCommand ToUpdateCommand(AiAssistantUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new AiAssistantUpdateCommand(
            input.BasicId,
            input.AssistantName,
            input.Avatar,
            input.Description,
            input.Greeting,
            input.PromptCode,
            input.ProviderCode,
            input.EnableKnowledge,
            input.KnowledgeProviderCode,
            input.KnowledgeTopK,
            input.HistoryRounds,
            input.IsDefault,
            input.IsEnabled,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射状态命令
    /// </summary>
    public static AiAssistantStatusChangeCommand ToStatusCommand(AiAssistantStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new AiAssistantStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 实体 → 列表项 DTO
    /// </summary>
    public static AiAssistantListItemDto ToListItemDto(SysAiAssistant entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Fill(new AiAssistantListItemDto(), entity);
    }

    /// <summary>
    /// 实体 → 详情 DTO
    /// </summary>
    public static AiAssistantDetailDto ToDetailDto(SysAiAssistant entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var dto = Fill(new AiAssistantDetailDto(), entity);
        dto.Greeting = entity.Greeting;
        dto.Remark = entity.Remark;
        dto.CreatedId = entity.CreatedId;
        dto.CreatedBy = entity.CreatedBy;
        dto.ModifiedId = entity.ModifiedId;
        dto.ModifiedBy = entity.ModifiedBy;
        return dto;
    }

    /// <summary>
    /// 实体 → 可用助手 DTO（聊天页选择用）
    /// </summary>
    public static AiAssistantOptionDto ToOptionDto(SysAiAssistant entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AiAssistantOptionDto
        {
            BasicId = entity.BasicId,
            AssistantCode = entity.AssistantCode,
            AssistantName = entity.AssistantName,
            Avatar = entity.Avatar,
            Description = entity.Description,
            IsDefault = entity.IsDefault
        };
    }

    private static TDto Fill<TDto>(TDto dto, SysAiAssistant entity)
        where TDto : AiAssistantListItemDto
    {
        dto.BasicId = entity.BasicId;
        dto.AssistantCode = entity.AssistantCode;
        dto.AssistantName = entity.AssistantName;
        dto.Avatar = entity.Avatar;
        dto.Description = entity.Description;
        dto.PromptCode = entity.PromptCode;
        dto.ProviderCode = entity.ProviderCode;
        dto.EnableKnowledge = entity.EnableKnowledge;
        dto.KnowledgeProviderCode = entity.KnowledgeProviderCode;
        dto.KnowledgeTopK = entity.KnowledgeTopK;
        dto.HistoryRounds = entity.HistoryRounds;
        dto.IsDefault = entity.IsDefault;
        dto.IsEnabled = entity.IsEnabled;
        dto.Sort = entity.Sort;
        dto.Status = entity.Status;
        dto.CreatedTime = entity.CreatedTime;
        dto.ModifiedTime = entity.ModifiedTime;
        return dto;
    }
}
