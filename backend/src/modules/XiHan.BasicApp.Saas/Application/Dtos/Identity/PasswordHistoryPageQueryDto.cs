// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 密码历史分页查询 DTO
/// </summary>
public sealed class PasswordHistoryPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 修改开始时间
    /// </summary>
    public DateTimeOffset? ChangedTimeStart { get; set; }

    /// <summary>
    /// 修改结束时间
    /// </summary>
    public DateTimeOffset? ChangedTimeEnd { get; set; }
}
