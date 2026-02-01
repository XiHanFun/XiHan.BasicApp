#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacDtoBase
// Guid:1a2b3c4d-5e6f-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 03:58:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Dtos.Base;

/// <summary>
/// RBAC DTO 基类
/// </summary>
public abstract class RbacDtoBase
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// RBAC 创建 DTO 基类
/// </summary>
public abstract class RbacCreationDtoBase
{
}

/// <summary>
/// RBAC 更新 DTO 基类
/// </summary>
public abstract class RbacUpdateDtoBase
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// RBAC 完整审计 DTO 基类
/// </summary>
public abstract class RbacFullAuditedDtoBase : RbacDtoBase
{
    /// <summary>
    /// 创建者ID
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改者ID
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 删除者ID
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTimeOffset? DeletedTime { get; set; }
}
