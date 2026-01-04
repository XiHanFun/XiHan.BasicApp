#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenDtoBase
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567020
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.CodeGeneration.Dtos.Base;

/// <summary>
/// CodeGen DTO 基类
/// </summary>
public abstract class CodeGenDtoBase
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// CodeGen 创建 DTO 基类
/// </summary>
public abstract class CodeGenCreationDtoBase
{
}

/// <summary>
/// CodeGen 更新 DTO 基类
/// </summary>
public abstract class CodeGenUpdateDtoBase
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// CodeGen 完整审计 DTO 基类
/// </summary>
public abstract class CodeGenFullAuditedDtoBase : CodeGenDtoBase
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
