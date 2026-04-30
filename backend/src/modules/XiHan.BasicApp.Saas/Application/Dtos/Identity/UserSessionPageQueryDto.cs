#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionPageQueryDto
// Guid:6f2e7b68-56ef-43f5-a109-f1d8ad04a420
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// 是否在线
    /// </summary>
    public bool? IsOnline { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool? IsRevoked { get; set; }

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
