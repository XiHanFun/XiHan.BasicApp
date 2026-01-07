#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionQueryService
// Guid:f2a3b4c5-d6e7-4f8a-9b0c-1d2e3f4a5b6c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 用户会话查询服务（处理用户会话的读操作 - CQRS）
/// </summary>
public class UserSessionQueryService : ApplicationServiceBase
{
    private readonly IUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionQueryService(IUserSessionRepository userSessionRepository)
    {
        _userSessionRepository = userSessionRepository;
    }

    /// <summary>
    /// 根据ID获取会话
    /// </summary>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var session = await _userSessionRepository.GetByIdAsync(id);
        return session?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据会话Token获取会话
    /// </summary>
    public async Task<RbacDtoBase?> GetBySessionTokenAsync(string sessionToken)
    {
        var session = await _userSessionRepository.GetBySessionTokenAsync(sessionToken);
        return session?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户的所有活跃会话
    /// </summary>
    public async Task<List<RbacDtoBase>> GetActiveSessionsByUserIdAsync(long userId)
    {
        var sessions = await _userSessionRepository.GetActiveSessionsByUserIdAsync(userId);
        return sessions.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 验证会话是否有效
    /// </summary>
    public async Task<bool> ValidateSessionAsync(string sessionToken)
    {
        return await _userSessionRepository.ValidateSessionAsync(sessionToken);
    }

    /// <summary>
    /// 获取用户的会话统计信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>会话统计（活跃数、总数等）</returns>
    public async Task<Dictionary<string, int>> GetSessionStatsAsync(long userId)
    {
        var allSessions = await _userSessionRepository.GetActiveSessionsByUserIdAsync(userId);
        
        return new Dictionary<string, int>
        {
            { "ActiveCount", allSessions.Count },
            { "TotalDevices", allSessions.Select(s => s.DeviceId).Distinct().Count() }
        };
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _userSessionRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
