#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResourceDto
// Guid:f6a7b8c9-d0e1-2345-6789-0f01234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统资源创建 DTO
/// </summary>
public class SysResourceCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 父资源ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 资源编码
    /// </summary>
    public string ResourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType ResourceType { get; set; } = ResourceType.Menu;

    /// <summary>
    /// 资源路径
    /// </summary>
    public string? ResourcePath { get; set; }

    /// <summary>
    /// 资源图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 资源描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 资源元数据
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 是否需要认证
    /// </summary>
    public bool RequireAuth { get; set; } = true;

    /// <summary>
    /// 是否公开资源
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统资源更新 DTO
/// </summary>
public class SysResourceUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 父资源ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 资源编码
    /// </summary>
    public string ResourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType ResourceType { get; set; } = ResourceType.Menu;

    /// <summary>
    /// 资源路径
    /// </summary>
    public string? ResourcePath { get; set; }

    /// <summary>
    /// 资源图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 资源描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 资源元数据
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 是否需要认证
    /// </summary>
    public bool RequireAuth { get; set; } = true;

    /// <summary>
    /// 是否公开资源
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统资源查询 DTO
/// </summary>
public class SysResourceGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 父资源ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 资源编码
    /// </summary>
    public string ResourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType ResourceType { get; set; } = ResourceType.Menu;

    /// <summary>
    /// 资源路径
    /// </summary>
    public string? ResourcePath { get; set; }

    /// <summary>
    /// 资源图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 资源描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 资源元数据
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 是否需要认证
    /// </summary>
    public bool RequireAuth { get; set; } = true;

    /// <summary>
    /// 是否公开资源
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
