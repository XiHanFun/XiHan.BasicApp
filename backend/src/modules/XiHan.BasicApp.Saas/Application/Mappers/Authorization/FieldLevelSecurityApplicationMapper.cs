// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 字段级安全应用层映射器
/// </summary>
public static class FieldLevelSecurityApplicationMapper
{
    /// <summary>
    /// 映射字段级安全创建命令
    /// </summary>
    public static FieldLevelSecurityCreateCommand ToCreateCommand(FieldLevelSecurityCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new FieldLevelSecurityCreateCommand(
            input.TargetType,
            input.TargetId,
            input.ResourceId,
            input.FieldName,
            input.IsReadable,
            input.IsEditable,
            input.MaskStrategy,
            input.MaskPattern,
            input.Priority,
            input.Description,
            input.Status,
            input.Remark);
    }

    /// <summary>
    /// 映射字段级安全更新命令
    /// </summary>
    public static FieldLevelSecurityUpdateCommand ToUpdateCommand(FieldLevelSecurityUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new FieldLevelSecurityUpdateCommand(
            input.BasicId,
            input.TargetType,
            input.TargetId,
            input.ResourceId,
            input.FieldName,
            input.IsReadable,
            input.IsEditable,
            input.MaskStrategy,
            input.MaskPattern,
            input.Priority,
            input.Description,
            input.Remark);
    }

    /// <summary>
    /// 映射字段级安全状态命令
    /// </summary>
    public static FieldLevelSecurityStatusChangeCommand ToStatusCommand(FieldLevelSecurityStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new FieldLevelSecurityStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射字段级安全列表项
    /// </summary>
    /// <param name="policy">字段级安全策略</param>
    /// <param name="resource">资源定义</param>
    /// <param name="targetCode">目标编码</param>
    /// <param name="targetName">目标名称</param>
    /// <returns>字段级安全列表项 DTO</returns>
    public static FieldLevelSecurityListItemDto ToListItemDto(
        SysFieldLevelSecurity policy,
        SysResource? resource,
        string? targetCode,
        string? targetName)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new FieldLevelSecurityListItemDto
        {
            BasicId = policy.BasicId,
            TargetType = policy.TargetType,
            TargetId = policy.TargetId,
            TargetCode = targetCode,
            TargetName = targetName,
            ResourceId = policy.ResourceId,
            ResourceCode = resource?.ResourceCode,
            ResourceName = resource?.ResourceName,
            ResourceType = resource?.ResourceType,
            FieldName = policy.FieldName,
            IsReadable = policy.IsReadable,
            IsEditable = policy.IsEditable,
            MaskStrategy = policy.MaskStrategy,
            Priority = policy.Priority,
            Description = policy.Description,
            Status = policy.Status,
            CreatedTime = policy.CreatedTime,
            ModifiedTime = policy.ModifiedTime
        };
    }

    /// <summary>
    /// 映射字段级安全详情
    /// </summary>
    /// <param name="policy">字段级安全策略</param>
    /// <param name="resource">资源定义</param>
    /// <param name="targetCode">目标编码</param>
    /// <param name="targetName">目标名称</param>
    /// <returns>字段级安全详情 DTO</returns>
    public static FieldLevelSecurityDetailDto ToDetailDto(
        SysFieldLevelSecurity policy,
        SysResource? resource,
        string? targetCode,
        string? targetName)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new FieldLevelSecurityDetailDto
        {
            BasicId = policy.BasicId,
            TargetType = policy.TargetType,
            TargetId = policy.TargetId,
            TargetCode = targetCode,
            TargetName = targetName,
            ResourceId = policy.ResourceId,
            ResourceCode = resource?.ResourceCode,
            ResourceName = resource?.ResourceName,
            ResourceType = resource?.ResourceType,
            FieldName = policy.FieldName,
            IsReadable = policy.IsReadable,
            IsEditable = policy.IsEditable,
            MaskStrategy = policy.MaskStrategy,
            MaskPattern = policy.MaskPattern,
            Priority = policy.Priority,
            Description = policy.Description,
            Status = policy.Status,
            Remark = policy.Remark,
            CreatedTime = policy.CreatedTime,
            CreatedId = policy.CreatedId,
            CreatedBy = policy.CreatedBy,
            ModifiedTime = policy.ModifiedTime,
            ModifiedId = policy.ModifiedId,
            ModifiedBy = policy.ModifiedBy
        };
    }
}
