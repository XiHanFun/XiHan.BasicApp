#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ApiLogListItemDto
// Guid:57b19e48-0634-4c38-a33f-4e8e340224b4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// API 日志列表项 DTO
/// </summary>
public class ApiLogListItemDto : BasicAppDto
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
    /// 客户端标识
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 应用标识
    /// </summary>
    public string? AppId { get; set; }

    /// <summary>
    /// API 路径
    /// </summary>
    public string ApiPath { get; set; } = string.Empty;

    /// <summary>
    /// API 名称
    /// </summary>
    public string? ApiName { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 控制器名称
    /// </summary>
    public string? ControllerName { get; set; }

    /// <summary>
    /// 操作名称
    /// </summary>
    public string? ActionName { get; set; }

    /// <summary>
    /// 签名是否有效
    /// </summary>
    public bool IsSignatureValid { get; set; }

    /// <summary>
    /// 签名类型
    /// </summary>
    public SignatureType SignatureType { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 请求 IP
    /// </summary>
    public string? RequestIp { get; set; }

    /// <summary>
    /// 请求地址
    /// </summary>
    public string? RequestLocation { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 请求来源
    /// </summary>
    public string? Referer { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTimeOffset RequestTime { get; set; }

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTimeOffset? ResponseTime { get; set; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public long ExecutionTime { get; set; }

    /// <summary>
    /// 请求大小（字节）
    /// </summary>
    public long RequestSize { get; set; }

    /// <summary>
    /// 响应大小（字节）
    /// </summary>
    public long ResponseSize { get; set; }

    /// <summary>
    /// API 版本
    /// </summary>
    public string? ApiVersion { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public string? RequestParams { get; set; }

    /// <summary>
    /// 请求体
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 响应体
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// 请求头
    /// </summary>
    public string? RequestHeaders { get; set; }

    /// <summary>
    /// 响应头
    /// </summary>
    public string? ResponseHeaders { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

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
