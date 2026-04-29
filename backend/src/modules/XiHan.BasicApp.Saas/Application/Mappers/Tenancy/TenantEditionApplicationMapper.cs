#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionApplicationMapper
// Guid:3cce6b57-88ea-42ef-b76f-ef733de5a7e8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 租户版本应用层映射器
/// </summary>
public static class TenantEditionApplicationMapper
{
    /// <summary>
    /// 映射租户版本列表项
    /// </summary>
    /// <param name="edition">租户版本</param>
    /// <returns>租户版本列表项 DTO</returns>
    public static TenantEditionListItemDto ToListItemDto(SysTenantEdition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);

        return new TenantEditionListItemDto
        {
            BasicId = edition.BasicId,
            EditionCode = edition.EditionCode,
            EditionName = edition.EditionName,
            Description = edition.Description,
            UserLimit = edition.UserLimit,
            StorageLimit = edition.StorageLimit,
            Price = edition.Price,
            BillingPeriodMonths = edition.BillingPeriodMonths,
            IsFree = edition.IsFree,
            IsDefault = edition.IsDefault,
            Status = edition.Status,
            Sort = edition.Sort,
            CreatedTime = edition.CreatedTime,
            ModifiedTime = edition.ModifiedTime
        };
    }

    /// <summary>
    /// 映射租户版本详情
    /// </summary>
    /// <param name="edition">租户版本</param>
    /// <returns>租户版本详情 DTO</returns>
    public static TenantEditionDetailDto ToDetailDto(SysTenantEdition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);

        return new TenantEditionDetailDto
        {
            BasicId = edition.BasicId,
            EditionCode = edition.EditionCode,
            EditionName = edition.EditionName,
            Description = edition.Description,
            UserLimit = edition.UserLimit,
            StorageLimit = edition.StorageLimit,
            Price = edition.Price,
            BillingPeriodMonths = edition.BillingPeriodMonths,
            IsFree = edition.IsFree,
            IsDefault = edition.IsDefault,
            Status = edition.Status,
            Sort = edition.Sort,
            Remark = edition.Remark,
            CreatedTime = edition.CreatedTime,
            CreatedId = edition.CreatedId,
            CreatedBy = edition.CreatedBy,
            ModifiedTime = edition.ModifiedTime,
            ModifiedId = edition.ModifiedId,
            ModifiedBy = edition.ModifiedBy
        };
    }
}
