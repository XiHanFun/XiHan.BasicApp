#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionDetailDto
// Guid:debc2b50-b8ed-4626-bb85-2505e5cd821c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限 ABAC 条件详情 DTO
/// </summary>
public sealed class PermissionConditionDetailDto : PermissionConditionListItemDto
{
    /// <summary>
    /// 创建人主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }

    /// <summary>
    /// 修改人主键
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public string? ModifiedBy { get; set; }
}
