#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExceptionLogDto
// Guid:a1b2c3d4-0001-0003-0001-000000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/10 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Application.Contracts.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 异常日志 DTO
/// </summary>
public class ExceptionLogDto : DtoBase<long>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 请求ID
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 链路ID
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 异常类型
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// 异常消息
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 内部异常类型
    /// </summary>
    public string? InnerExceptionType { get; set; }

    /// <summary>
    /// 内部异常消息
    /// </summary>
    public string? InnerExceptionMessage { get; set; }

    /// <summary>
    /// 内部异常堆栈
    /// </summary>
    public string? InnerExceptionStackTrace { get; set; }

    /// <summary>
    /// 异常来源
    /// </summary>
    public string? ExceptionSource { get; set; }

    /// <summary>
    /// 异常位置
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
    /// HTTP 状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 操作IP
    /// </summary>
    public string? OperationIp { get; set; }

    /// <summary>
    /// 操作地区
    /// </summary>
    public string? OperationLocation { get; set; }

    /// <summary>
    /// 用户代理
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
    public string? DeviceType { get; set; }

    /// <summary>
    /// 设备信息
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// 应用版本
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
    /// 线程ID
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// 进程ID
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
    /// 处理人ID
    /// </summary>
    public long? HandledBy { get; set; }

    /// <summary>
    /// 处理人名称
    /// </summary>
    public string? HandledByName { get; set; }

    /// <summary>
    /// 处理备注
    /// </summary>
    public string? HandledRemark { get; set; }

    /// <summary>
    /// 业务模块
    /// </summary>
    public string? BusinessModule { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public string? BusinessId { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 错误码
    /// </summary>
    public string? ErrorCode { get; set; }

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
