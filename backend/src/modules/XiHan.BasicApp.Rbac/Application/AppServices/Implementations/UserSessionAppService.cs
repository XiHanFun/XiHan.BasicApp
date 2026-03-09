#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionAppService
// Guid:2093cce8-4fbe-4490-9bcb-28738f6454a4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:58:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 用户会话应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class UserSessionAppService
    : CrudApplicationServiceBase<SysUserSession, UserSessionDto, long, UserSessionCreateDto, UserSessionUpdateDto, BasicAppPRDto>,
        IUserSessionAppService
{
    private readonly IUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionAppService(IUserSessionRepository userSessionRepository)
        : base(userSessionRepository)
    {
        _userSessionRepository = userSessionRepository;
    }

    /// <summary>
    /// 根据会话ID获取会话
    /// </summary>
    public async Task<UserSessionDto?> GetBySessionIdAsync(string sessionId, long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var entity = await _userSessionRepository.GetBySessionIdAsync(sessionId, tenantId);
        return entity?.Adapt<UserSessionDto>();
    }

    /// <summary>
    /// 撤销用户会话
    /// </summary>
    public async Task<int> RevokeUserSessionsAsync(long userId, string reason, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("撤销原因不能为空", nameof(reason));
        }

        return await _userSessionRepository.RevokeUserSessionsAsync(userId, reason.Trim(), tenantId);
    }

    /// <summary>
    /// 创建用户会话
    /// </summary>
    public override async Task<UserSessionDto> CreateAsync(UserSessionCreateDto input)
    {
        input.ValidateAnnotations();
        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新用户会话
    /// </summary>
    public override async Task<UserSessionDto> UpdateAsync(UserSessionUpdateDto input)
    {
        input.ValidateAnnotations();
        return await base.UpdateAsync(input);
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    protected override Task<SysUserSession> MapDtoToEntityAsync(UserSessionCreateDto createDto)
    {
        var entity = new SysUserSession
        {
            TenantId = createDto.TenantId,
            UserId = createDto.UserId,
            CurrentAccessTokenJti = createDto.CurrentAccessTokenJti,
            UserSessionId = createDto.SessionId.Trim(),
            DeviceType = createDto.DeviceType,
            DeviceName = createDto.DeviceName,
            DeviceId = createDto.DeviceId,
            OperatingSystem = createDto.OperatingSystem,
            Browser = createDto.Browser,
            IpAddress = createDto.IpAddress,
            Location = createDto.Location,
            LoginTime = createDto.LoginTime,
            LastActivityTime = createDto.LastActivityTime,
            IsOnline = createDto.IsOnline,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    protected override Task MapDtoToEntityAsync(UserSessionUpdateDto updateDto, SysUserSession entity)
    {
        entity.CurrentAccessTokenJti = updateDto.CurrentAccessTokenJti;
        entity.DeviceType = updateDto.DeviceType;
        entity.DeviceName = updateDto.DeviceName;
        entity.DeviceId = updateDto.DeviceId;
        entity.OperatingSystem = updateDto.OperatingSystem;
        entity.Browser = updateDto.Browser;
        entity.IpAddress = updateDto.IpAddress;
        entity.Location = updateDto.Location;
        entity.LastActivityTime = updateDto.LastActivityTime;
        entity.IsOnline = updateDto.IsOnline;
        entity.IsRevoked = updateDto.IsRevoked;
        entity.RevokedAt = updateDto.RevokedAt;
        entity.RevokedReason = updateDto.RevokedReason;
        entity.LogoutTime = updateDto.LogoutTime;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }
}
