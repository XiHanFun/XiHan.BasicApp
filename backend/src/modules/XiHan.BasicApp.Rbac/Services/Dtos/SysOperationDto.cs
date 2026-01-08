#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperationDto
// Guid:a7b8c9d0-e1f2-3456-7890-1234567890abc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统操作创建 DTO
/// </summary>
public class SysOperationCreateDto : RbacCreationDtoBase
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
    /// HTTP方法
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
    public bool IsDangerous { get; set; } = false;

    /// <summary>
    /// 是否需要审计日志
    /// </summary>
    public bool RequireAudit { get; set; } = false;

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
/// 系统操作更新 DTO
/// </summary>
public class SysOperationUpdateDto : RbacUpdateDtoBase
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
    /// HTTP方法
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
    public bool IsDangerous { get; set; } = false;

    /// <summary>
    /// 是否需要审计日志
    /// </summary>
    public bool RequireAudit { get; set; } = false;

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
/// 系统操作查询 DTO
/// </summary>
public class SysOperationGetDto : RbacFullAuditedDtoBase
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
    /// HTTP方法
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
    public bool IsDangerous { get; set; } = false;

    /// <summary>
    /// 是否需要审计日志
    /// </summary>
    public bool RequireAudit { get; set; } = false;

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
