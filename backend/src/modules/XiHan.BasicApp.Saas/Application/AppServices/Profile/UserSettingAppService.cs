// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户设置应用服务（写侧：按 用户 × 场景 × 设置键 upsert，写后失效该用户设置读缓存，
/// 并向该用户全部在线连接推送 UserSettingChanged 实现多端实时同步）。
/// 不解释 SettingValue 语义，仅作个人设置载荷的跨端持久化。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户设置")]
public sealed class UserSettingAppService
    : SaasApplicationService, IUserSettingAppService
{
    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly ICurrentUser _currentUser;

    private readonly ILogger<UserSettingAppService> _logger;

    private readonly IRealtimeNotificationService<BasicAppNotificationHub> _realtimeNotificationService;

    private readonly IUserSettingRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSettingAppService(
        IUserSettingRepository repository,
        ICurrentUser currentUser,
        ISaasCacheInvalidator cacheInvalidator,
        IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotificationService,
        ILogger<UserSettingAppService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _cacheInvalidator = cacheInvalidator;
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserSettingDto> SaveAsync(UserSettingSaveDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.SettingKey))
        {
            throw new ArgumentException("设置键不能为空。", nameof(input));
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var settingKey = input.SettingKey.Trim();

        var entity = await _repository.GetByUserSettingAsync(userId, input.Scene, settingKey, cancellationToken);
        if (entity is null)
        {
            entity = new SysUserSetting
            {
                UserId = userId,
                Scene = input.Scene,
                SettingKey = settingKey,
                SettingValue = input.SettingValue
            };
            _ = await _repository.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.SettingValue = input.SettingValue;

            // 用户主体数据自有行写入：SysUserSetting 按 UserId 归属（唯一索引 UserId+Scene+SettingKey 不含租户），
            // 平台归属用户（行 TenantId=0）在租户态保存自己的偏好是合法路径，须显式豁免写路径租户边界
            using (TenantWriteGuard.Suppress())
            {
                _ = await _repository.UpdateAsync(entity, cancellationToken);
            }
        }

        // 写后失效该用户设置读缓存，保证下次读取拿到最新值
        await _cacheInvalidator.InvalidateUserSettingAsync(userId, cancellationToken);

        // 向该用户全部在线连接推送变更（含发起端，前端按 sourceClientId 过滤自身回显）；失败只记日志，不阻断保存
        try
        {
            await _realtimeNotificationService.SendToUserAsync(
                userId.ToString(),
                SignalRConstants.ClientMethods.UserSettingChanged,
                new
                {
                    scene = (int)input.Scene,
                    settingKey,
                    settingValue = input.SettingValue,
                    sourceClientId = input.ClientId
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "用户设置变更推送失败，UserId={UserId}，Scene={Scene}，SettingKey={SettingKey}", userId, input.Scene, settingKey);
        }

        return new UserSettingDto
        {
            Scene = input.Scene,
            SettingKey = settingKey,
            SettingValue = input.SettingValue
        };
    }
}
