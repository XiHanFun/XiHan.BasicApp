#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationLogDto
// Guid:e1c2d3e4-f5a6-7890-abcd-ef1234567896
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.OperationLogs.Dtos;

/// <summary>
/// 操作日志 DTO
/// </summary>
public class OperationLogDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public XiHanBasicAppIdType? TenantId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public XiHanBasicAppIdType? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public OperationType OperationType { get; set; } = OperationType.Other;

    /// <summary>
    /// 操作模块
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// 操作功能
    /// </summary>
    public string? Function { get; set; }

    /// <summary>
    /// 操作标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 请求URL
    /// </summary>
    public string? RequestUrl { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public string? RequestParams { get; set; }

    /// <summary>
    /// 响应结果
    /// </summary>
    public string? ResponseResult { get; set; }

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    public XiHanBasicAppIdType ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 操作IP
    /// </summary>
    public string? OperationIp { get; set; }

    /// <summary>
    /// 操作地址
    /// </summary>
    public string? OperationLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 操作状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTimeOffset OperationTime { get; set; } = DateTimeOffset.Now;
}
