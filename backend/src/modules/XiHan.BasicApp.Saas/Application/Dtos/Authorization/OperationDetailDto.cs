#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationDetailDto
// Guid:8a09c74e-1536-4fdf-b9c6-d179bdf88e9e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 操作详情 DTO
/// </summary>
public sealed class OperationDetailDto : BasicAppDto
{
    /// <summary>
    /// 操作编码
    /// </summary>
    public string OperationCode { get; set; } = string.Empty;

    /// <summary>
    /// 操作名称
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型代码
    /// </summary>
    public OperationTypeCode OperationTypeCode { get; set; }

    /// <summary>
    /// 操作分类
    /// </summary>
    public OperationCategory Category { get; set; }

    /// <summary>
    /// HTTP 方法
    /// </summary>
    public HttpMethodType? HttpMethod { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 操作图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 操作颜色
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 是否危险操作
    /// </summary>
    public bool IsDangerous { get; set; }

    /// <summary>
    /// 是否需要审计
    /// </summary>
    public bool IsRequireAudit { get; set; }

    /// <summary>
    /// 是否全局操作
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

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
