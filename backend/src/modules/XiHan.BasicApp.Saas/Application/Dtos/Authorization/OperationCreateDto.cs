#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationCreateDto
// Guid:594c9f2f-2d37-4c63-b96c-4c6f9f7d4a1b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 操作定义创建 DTO
/// </summary>
public sealed class OperationCreateDto
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
    public OperationTypeCode OperationTypeCode { get; set; } = OperationTypeCode.Read;

    /// <summary>
    /// 操作分类
    /// </summary>
    public OperationCategory Category { get; set; } = OperationCategory.Crud;

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
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
