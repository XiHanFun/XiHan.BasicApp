#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExceptionLogPageQueryDto
// Guid:ddc90863-0ce6-45f4-ae4d-6c5c6adf3954
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 异常日志分页查询 DTO
/// </summary>
public sealed class ExceptionLogPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 会话标识
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 请求标识
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 链路追踪 ID
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 异常类型
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// 异常源
    /// </summary>
    public string? ExceptionSource { get; set; }

    /// <summary>
    /// 异常发生位置
    /// </summary>
    public string? ExceptionLocation { get; set; }

    /// <summary>
    /// 严重级别
    /// </summary>
    public int? SeverityLevel { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType? DeviceType { get; set; }

    /// <summary>
    /// 应用程序名称
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// 应用程序版本
    /// </summary>
    public string? ApplicationVersion { get; set; }

    /// <summary>
    /// 环境名称
    /// </summary>
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// 是否已处理
    /// </summary>
    public bool? IsHandled { get; set; }

    /// <summary>
    /// 处理人主键
    /// </summary>
    public long? HandledBy { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 异常开始时间
    /// </summary>
    public DateTimeOffset? ExceptionTimeStart { get; set; }

    /// <summary>
    /// 异常结束时间
    /// </summary>
    public DateTimeOffset? ExceptionTimeEnd { get; set; }
}
