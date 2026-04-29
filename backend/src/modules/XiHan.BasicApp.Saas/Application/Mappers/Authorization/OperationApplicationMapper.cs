#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationApplicationMapper
// Guid:8e232fa4-e630-4c18-b1a4-d6045013d9ac
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 操作应用层映射器
/// </summary>
public static class OperationApplicationMapper
{
    /// <summary>
    /// 映射操作列表项
    /// </summary>
    /// <param name="operation">操作定义</param>
    /// <returns>操作列表项 DTO</returns>
    public static OperationListItemDto ToListItemDto(SysOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return new OperationListItemDto
        {
            BasicId = operation.BasicId,
            OperationCode = operation.OperationCode,
            OperationName = operation.OperationName,
            OperationTypeCode = operation.OperationTypeCode,
            Category = operation.Category,
            HttpMethod = operation.HttpMethod,
            Description = operation.Description,
            Icon = operation.Icon,
            Color = operation.Color,
            IsDangerous = operation.IsDangerous,
            IsRequireAudit = operation.IsRequireAudit,
            IsGlobal = operation.IsGlobal,
            Status = operation.Status,
            Sort = operation.Sort,
            CreatedTime = operation.CreatedTime,
            ModifiedTime = operation.ModifiedTime
        };
    }

    /// <summary>
    /// 映射操作详情
    /// </summary>
    /// <param name="operation">操作定义</param>
    /// <returns>操作详情 DTO</returns>
    public static OperationDetailDto ToDetailDto(SysOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return new OperationDetailDto
        {
            BasicId = operation.BasicId,
            OperationCode = operation.OperationCode,
            OperationName = operation.OperationName,
            OperationTypeCode = operation.OperationTypeCode,
            Category = operation.Category,
            HttpMethod = operation.HttpMethod,
            Description = operation.Description,
            Icon = operation.Icon,
            Color = operation.Color,
            IsDangerous = operation.IsDangerous,
            IsRequireAudit = operation.IsRequireAudit,
            IsGlobal = operation.IsGlobal,
            Status = operation.Status,
            Sort = operation.Sort,
            Remark = operation.Remark,
            CreatedTime = operation.CreatedTime,
            CreatedId = operation.CreatedId,
            CreatedBy = operation.CreatedBy,
            ModifiedTime = operation.ModifiedTime,
            ModifiedId = operation.ModifiedId,
            ModifiedBy = operation.ModifiedBy
        };
    }

    /// <summary>
    /// 映射操作选择项
    /// </summary>
    /// <param name="operation">操作定义</param>
    /// <returns>操作选择项 DTO</returns>
    public static OperationSelectItemDto ToSelectItemDto(SysOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return new OperationSelectItemDto
        {
            BasicId = operation.BasicId,
            OperationCode = operation.OperationCode,
            OperationName = operation.OperationName,
            OperationTypeCode = operation.OperationTypeCode,
            Category = operation.Category,
            HttpMethod = operation.HttpMethod,
            IsDangerous = operation.IsDangerous,
            IsRequireAudit = operation.IsRequireAudit
        };
    }
}
