// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 异常日志列表项 DTO
/// </summary>
public class ExceptionLogListItemDto : BasicAppDto
{
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
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// 异常消息
    /// </summary>
    public string ExceptionMessage { get; set; } = string.Empty;

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
    public int SeverityLevel { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// 控制器名称
    /// </summary>
    public string? ControllerName { get; set; }

    /// <summary>
    /// 操作名称
    /// </summary>
    public string? ActionName { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 操作 IP
    /// </summary>
    public string? OperationIp { get; set; }

    /// <summary>
    /// 操作地址
    /// </summary>
    public string? OperationLocation { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// 设备信息
    /// </summary>
    public string? DeviceInfo { get; set; }

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
    /// 服务器主机名
    /// </summary>
    public string? ServerHostName { get; set; }

    /// <summary>
    /// 线程 ID
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// 进程 ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// 异常时间
    /// </summary>
    public DateTimeOffset ExceptionTime { get; set; }

    /// <summary>
    /// 是否已处理
    /// </summary>
    public bool IsHandled { get; set; }

    /// <summary>
    /// 处理时间
    /// </summary>
    public DateTimeOffset? HandledTime { get; set; }

    /// <summary>
    /// 处理人主键
    /// </summary>
    public long? HandledBy { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 处理备注
    /// </summary>
    public string? HandledRemark { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public string? RequestParams { get; set; }

    /// <summary>
    /// 请求体
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 请求头
    /// </summary>
    public string? RequestHeaders { get; set; }

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
