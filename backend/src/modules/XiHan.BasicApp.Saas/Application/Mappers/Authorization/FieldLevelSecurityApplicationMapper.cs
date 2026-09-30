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
    /// 映射创建命令
    /// </summary>
    public static FieldLevelSecurityCreateCommand ToCreateCommand(FieldLevelSecurityCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new FieldLevelSecurityCreateCommand(
            input.TargetType,
            input.TargetId,
            input.EntityName,
            input.FieldName,
            input.MaskStrategy,
            input.MaskKeepHead,
            input.MaskKeepTail,
            input.MaskReplacement,
            input.IsEditable,
            input.Status,
            input.Remark);
    }

    /// <summary>
    /// 映射更新命令
    /// </summary>
    public static FieldLevelSecurityUpdateCommand ToUpdateCommand(FieldLevelSecurityUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new FieldLevelSecurityUpdateCommand(
            input.BasicId,
            input.TargetType,
            input.TargetId,
            input.EntityName,
            input.FieldName,
            input.MaskStrategy,
            input.MaskKeepHead,
            input.MaskKeepTail,
            input.MaskReplacement,
            input.IsEditable,
            input.Remark);
    }

    /// <summary>
    /// 映射状态命令
    /// </summary>
    public static FieldLevelSecurityStatusChangeCommand ToStatusCommand(FieldLevelSecurityStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new FieldLevelSecurityStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射列表项
    /// </summary>
    public static FieldLevelSecurityListItemDto ToListItemDto(
        SysFieldLevelSecurity policy,
        IFieldSecurityEntityCatalog catalog,
        string? targetCode,
        string? targetName)
    {
        return Fill(new FieldLevelSecurityListItemDto(), policy, catalog, targetCode, targetName);
    }

    /// <summary>
    /// 映射详情
    /// </summary>
    public static FieldLevelSecurityDetailDto ToDetailDto(
        SysFieldLevelSecurity policy,
        IFieldSecurityEntityCatalog catalog,
        string? targetCode,
        string? targetName)
    {
        var detail = Fill(new FieldLevelSecurityDetailDto(), policy, catalog, targetCode, targetName);
        detail.CreatedId = policy.CreatedId;
        detail.CreatedBy = policy.CreatedBy;
        detail.ModifiedId = policy.ModifiedId;
        detail.ModifiedBy = policy.ModifiedBy;
        return detail;
    }

    /// <summary>
    /// 映射可配置实体
    /// </summary>
    public static FieldSecurityEntityDto ToEntityDto(FieldSecurityEntityDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return new FieldSecurityEntityDto
        {
            EntityName = descriptor.EntityName,
            DisplayName = descriptor.DisplayName,
            Fields =
            [
                .. descriptor.Fields.Select(field => new FieldSecurityFieldDto
                {
                    FieldName = field.FieldName,
                    DisplayName = field.DisplayName,
                    IsText = field.IsText
                })
            ]
        };
    }

    /// <summary>
    /// 填充列表与详情共有字段；实体或字段已随版本移除时显示名为空，页面据此提示规则失效
    /// </summary>
    private static TDto Fill<TDto>(
        TDto dto,
        SysFieldLevelSecurity policy,
        IFieldSecurityEntityCatalog catalog,
        string? targetCode,
        string? targetName)
        where TDto : FieldLevelSecurityListItemDto
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(catalog);

        var entity = catalog.Find(policy.EntityName);
        dto.BasicId = policy.BasicId;
        dto.TargetType = policy.TargetType;
        dto.TargetId = policy.TargetId;
        dto.TargetCode = targetCode;
        dto.TargetName = targetName;
        dto.EntityName = policy.EntityName;
        dto.EntityDisplayName = entity?.DisplayName;
        dto.FieldName = policy.FieldName;
        dto.FieldDisplayName = entity?.FindField(policy.FieldName)?.DisplayName;
        dto.MaskStrategy = policy.MaskStrategy;
        dto.MaskKeepHead = policy.MaskKeepHead;
        dto.MaskKeepTail = policy.MaskKeepTail;
        dto.MaskReplacement = policy.MaskReplacement;
        dto.IsEditable = policy.IsEditable;
        dto.IsGlobal = policy.IsGlobal;
        dto.Status = policy.Status;
        dto.Remark = policy.Remark;
        dto.CreatedTime = policy.CreatedTime;
        dto.ModifiedTime = policy.ModifiedTime;
        return dto;
    }
}
