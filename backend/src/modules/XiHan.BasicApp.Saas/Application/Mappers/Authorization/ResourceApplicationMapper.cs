#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceApplicationMapper
// Guid:48bad79b-9d82-4911-b6d1-9c4ada0ad504
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 资源应用层映射器
/// </summary>
public static class ResourceApplicationMapper
{
    /// <summary>
    /// 映射资源列表项
    /// </summary>
    /// <param name="resource">资源定义</param>
    /// <returns>资源列表项 DTO</returns>
    public static ResourceListItemDto ToListItemDto(SysResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return new ResourceListItemDto
        {
            BasicId = resource.BasicId,
            ResourceCode = resource.ResourceCode,
            ResourceName = resource.ResourceName,
            ResourceType = resource.ResourceType,
            ResourcePath = resource.ResourcePath,
            Description = resource.Description,
            AccessLevel = resource.AccessLevel,
            IsGlobal = resource.IsGlobal,
            Status = resource.Status,
            Sort = resource.Sort,
            CreatedTime = resource.CreatedTime,
            ModifiedTime = resource.ModifiedTime
        };
    }

    /// <summary>
    /// 映射资源详情
    /// </summary>
    /// <param name="resource">资源定义</param>
    /// <returns>资源详情 DTO</returns>
    public static ResourceDetailDto ToDetailDto(SysResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return new ResourceDetailDto
        {
            BasicId = resource.BasicId,
            ResourceCode = resource.ResourceCode,
            ResourceName = resource.ResourceName,
            ResourceType = resource.ResourceType,
            ResourcePath = resource.ResourcePath,
            Description = resource.Description,
            Metadata = resource.Metadata,
            AccessLevel = resource.AccessLevel,
            IsGlobal = resource.IsGlobal,
            Status = resource.Status,
            Sort = resource.Sort,
            Remark = resource.Remark,
            CreatedTime = resource.CreatedTime,
            CreatedId = resource.CreatedId,
            CreatedBy = resource.CreatedBy,
            ModifiedTime = resource.ModifiedTime,
            ModifiedId = resource.ModifiedId,
            ModifiedBy = resource.ModifiedBy
        };
    }

    /// <summary>
    /// 映射资源选择项
    /// </summary>
    /// <param name="resource">资源定义</param>
    /// <returns>资源选择项 DTO</returns>
    public static ResourceSelectItemDto ToSelectItemDto(SysResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return new ResourceSelectItemDto
        {
            BasicId = resource.BasicId,
            ResourceCode = resource.ResourceCode,
            ResourceName = resource.ResourceName,
            ResourceType = resource.ResourceType,
            ResourcePath = resource.ResourcePath
        };
    }
}
