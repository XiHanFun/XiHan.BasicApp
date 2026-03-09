#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSessionAppService
// Guid:de8f3e89-1ed7-41d7-a8b0-022be1f32b0b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:42:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.AppServices;

/// <summary>
/// 用户会话应用服务
/// </summary>
public interface IUserSessionAppService
    : ICrudApplicationService<SysUserSession, UserSessionDto, long, UserSessionCreateDto, UserSessionUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据会话ID获取会话
    /// </summary>
    Task<UserSessionDto?> GetBySessionIdAsync(string sessionId, long? tenantId = null);

    /// <summary>
    /// 撤销用户会话
    /// </summary>
    Task<int> RevokeUserSessionsAsync(long userId, string reason, long? tenantId = null);
}
