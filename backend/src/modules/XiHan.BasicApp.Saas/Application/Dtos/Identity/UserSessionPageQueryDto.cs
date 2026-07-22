// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户会话分页查询 DTO
/// </summary>
public sealed class UserSessionPageQueryDto : BasicAppPRDto
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
    /// 设备类型
    /// </summary>
    public DeviceType? DeviceType { get; set; }

    /// <summary>
    /// 会话状态（Active=活跃 / Offline=离线 / Revoked=已撤销 / Expired=已过期）
    /// </summary>
    public SessionStatus? Status { get; set; }

    /// <summary>
    /// 登录开始时间
    /// </summary>
    public DateTimeOffset? LoginTimeStart { get; set; }

    /// <summary>
    /// 登录结束时间
    /// </summary>
    public DateTimeOffset? LoginTimeEnd { get; set; }

    /// <summary>
    /// 最后活动开始时间
    /// </summary>
    public DateTimeOffset? LastActivityTimeStart { get; set; }

    /// <summary>
    /// 最后活动结束时间
    /// </summary>
    public DateTimeOffset? LastActivityTimeEnd { get; set; }
}
