#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PasswordHistoryApplicationMapper
// Guid:8714a440-617c-412e-9fb2-3f29ee0b0b13
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 密码历史应用层映射器
/// </summary>
public static class PasswordHistoryApplicationMapper
{
    /// <summary>
    /// 映射密码历史列表项
    /// </summary>
    /// <param name="history">密码历史实体</param>
    /// <param name="user">用户实体</param>
    /// <returns>密码历史列表项 DTO</returns>
    public static PasswordHistoryListItemDto ToListItemDto(SysPasswordHistory history, SysUser? user)
    {
        ArgumentNullException.ThrowIfNull(history);

        return new PasswordHistoryListItemDto
        {
            BasicId = history.BasicId,
            UserId = history.UserId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            ChangedTime = history.ChangedTime,
            CreatedTime = history.CreatedTime
        };
    }

    /// <summary>
    /// 映射密码历史详情
    /// </summary>
    /// <param name="history">密码历史实体</param>
    /// <param name="user">用户实体</param>
    /// <returns>密码历史详情 DTO</returns>
    public static PasswordHistoryDetailDto ToDetailDto(SysPasswordHistory history, SysUser? user)
    {
        ArgumentNullException.ThrowIfNull(history);

        var item = ToListItemDto(history, user);
        return new PasswordHistoryDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            RealName = item.RealName,
            NickName = item.NickName,
            ChangedTime = item.ChangedTime,
            CreatedTime = item.CreatedTime,
            CreatedId = history.CreatedId,
            CreatedBy = history.CreatedBy
        };
    }
}
