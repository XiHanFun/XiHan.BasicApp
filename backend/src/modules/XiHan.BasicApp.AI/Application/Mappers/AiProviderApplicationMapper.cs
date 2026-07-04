#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiProviderApplicationMapper
// Guid:a11c0de0-4003-4a10-9a00-00000000ai43
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Entities;

namespace XiHan.BasicApp.AI.Application.Mappers;

/// <summary>
/// AI Provider 应用层映射器（手写静态映射，命令模式，对齐 Saas 约定）
/// </summary>
/// <remarks>密钥永不映射到任何读 DTO，仅以 <c>HasApiKey</c> 布尔标志暴露是否已配置。</remarks>
public static class AiProviderApplicationMapper
{
    /// <summary>
    /// 映射创建命令
    /// </summary>
    public static AiProviderCreateCommand ToCreateCommand(AiProviderCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new AiProviderCreateCommand(
            input.ConfigCode,
            input.ConfigName,
            input.Provider,
            input.Model,
            input.EmbeddingModel,
            input.BaseUrl,
            input.ApiKey,
            input.MaxOutputTokens,
            input.Temperature,
            input.TimeoutSeconds,
            input.ExtraJson,
            input.IsDefault,
            input.IsEnabled,
            input.Sort,
            input.Status,
            input.Remark);
    }

    /// <summary>
    /// 映射更新命令
    /// </summary>
    public static AiProviderUpdateCommand ToUpdateCommand(AiProviderUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new AiProviderUpdateCommand(
            input.BasicId,
            input.ConfigName,
            input.Provider,
            input.Model,
            input.EmbeddingModel,
            input.BaseUrl,
            input.ApiKey,
            input.MaxOutputTokens,
            input.Temperature,
            input.TimeoutSeconds,
            input.ExtraJson,
            input.IsDefault,
            input.IsEnabled,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射状态命令
    /// </summary>
    public static AiProviderStatusChangeCommand ToStatusCommand(AiProviderStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new AiProviderStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 实体 → 列表项 DTO
    /// </summary>
    public static AiProviderListItemDto ToListItemDto(SysAiProvider entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AiProviderListItemDto
        {
            BasicId = entity.BasicId,
            ConfigCode = entity.ConfigCode,
            ConfigName = entity.ConfigName,
            Provider = entity.Provider,
            Model = entity.Model,
            EmbeddingModel = entity.EmbeddingModel,
            BaseUrl = entity.BaseUrl,
            MaxOutputTokens = entity.MaxOutputTokens,
            Temperature = entity.Temperature,
            TimeoutSeconds = entity.TimeoutSeconds,
            IsDefault = entity.IsDefault,
            IsEnabled = entity.IsEnabled,
            HasApiKey = !string.IsNullOrEmpty(entity.ApiKey),
            Sort = entity.Sort,
            Status = entity.Status,
            CreatedTime = entity.CreatedTime,
            ModifiedTime = entity.ModifiedTime
        };
    }

    /// <summary>
    /// 实体 → 详情 DTO
    /// </summary>
    public static AiProviderDetailDto ToDetailDto(SysAiProvider entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var item = ToListItemDto(entity);
        return new AiProviderDetailDto
        {
            BasicId = item.BasicId,
            ConfigCode = item.ConfigCode,
            ConfigName = item.ConfigName,
            Provider = item.Provider,
            Model = item.Model,
            EmbeddingModel = item.EmbeddingModel,
            BaseUrl = item.BaseUrl,
            MaxOutputTokens = item.MaxOutputTokens,
            Temperature = item.Temperature,
            TimeoutSeconds = item.TimeoutSeconds,
            IsDefault = item.IsDefault,
            IsEnabled = item.IsEnabled,
            HasApiKey = item.HasApiKey,
            Sort = item.Sort,
            Status = item.Status,
            CreatedTime = item.CreatedTime,
            ModifiedTime = item.ModifiedTime,
            ExtraJson = entity.ExtraJson,
            Remark = entity.Remark,
            CreatedId = entity.CreatedId,
            CreatedBy = entity.CreatedBy,
            ModifiedId = entity.ModifiedId,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// 领域测试结果 → DTO
    /// </summary>
    public static AiProviderTestConnectionResultDto ToTestResultDto(AiProviderTestResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new AiProviderTestConnectionResultDto
        {
            Success = result.Success,
            Message = result.Message,
            LatencyMs = result.LatencyMs,
            Model = result.Model
        };
    }
}
