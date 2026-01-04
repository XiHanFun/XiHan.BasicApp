#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AccessLogDto
// Guid:b1c2d3e4-f5a6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.AccessLogs.Dtos;

/// <summary>
/// 访问日志 DTO
/// </summary>
public class AccessLogDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 访问资源路径
    /// </summary>
    public string ResourcePath { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// 资源类型
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 访问结果
    /// </summary>
    public AccessResult AccessResult { get; set; } = AccessResult.Success;

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// 访问IP
    /// </summary>
    public string? AccessIp { get; set; }

    /// <summary>
    /// 访问地址
    /// </summary>
    public string? AccessLocation { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public string? Device { get; set; }

    /// <summary>
    /// 访问来源
    /// </summary>
    public string? Referer { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTime { get; set; } = 0;

    /// <summary>
    /// 响应大小（字节）
    /// </summary>
    public long ResponseSize { get; set; } = 0;

    /// <summary>
    /// 访问时间
    /// </summary>
    public DateTimeOffset AccessTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 离开时间
    /// </summary>
    public DateTimeOffset? LeaveTime { get; set; }

    /// <summary>
    /// 停留时长（秒）
    /// </summary>
    public long StayTime { get; set; } = 0;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
