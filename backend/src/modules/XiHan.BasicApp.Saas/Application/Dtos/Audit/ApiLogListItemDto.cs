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
    /// 是否包含载荷内容
    /// </summary>
    public bool HasPayload { get; set; }

    /// <summary>
    /// 是否包含头部内容
    /// </summary>
    public bool HasHeaders { get; set; }

    /// <summary>
    /// 是否包含客户端上下文
    /// </summary>
    public bool HasClientContext { get; set; }

    /// <summary>
    /// 是否包含错误信息
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// 是否包含异常堆栈
    /// </summary>
    public bool HasException { get; set; }

    /// <summary>
    /// 是否包含扩展数据
    /// </summary>
    public bool HasExtension { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
