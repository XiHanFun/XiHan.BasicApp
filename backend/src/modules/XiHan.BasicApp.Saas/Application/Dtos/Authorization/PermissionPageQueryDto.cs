#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionPageQueryDto
// Guid:30f03c6c-8fcb-4f9a-a2fe-19a35e4fbf9b
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
/// 权限分页查询 DTO
/// </summary>
public sealed class PermissionPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（权限编码、名称、描述、标签）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 模块编码
    /// </summary>
    public string? ModuleCode { get; set; }

    /// <summary>
    /// 权限类型
    /// </summary>
    public PermissionType? PermissionType { get; set; }

    /// <summary>
    /// 资源主键
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 操作主键
    /// </summary>
    public long? OperationId { get; set; }

    /// <summary>
    /// 是否全局权限
    /// </summary>
    public bool? IsGlobal { get; set; }

    /// <summary>
    /// 是否需要审计
    /// </summary>
    public bool? IsRequireAudit { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
