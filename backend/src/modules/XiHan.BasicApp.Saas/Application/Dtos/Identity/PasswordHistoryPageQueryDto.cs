#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PasswordHistoryPageQueryDto
// Guid:8060cc7d-c526-4a33-82e1-3958d9c1f1f7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
