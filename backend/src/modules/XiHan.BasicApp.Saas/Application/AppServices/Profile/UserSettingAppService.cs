#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSettingAppService
// Guid:d3945670-1325-4172-9e90-8fa341728cd9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户设置应用服务（写侧：按 用户 × 场景 × 设置键 upsert，写后失效该用户设置读缓存）。
/// 不解释 SettingValue 语义，仅作个人设置载荷的跨端持久化。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户设置")]
public sealed class UserSettingAppService
    : SaasApplicationService, IUserSettingAppService
{
    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly ICurrentUser _currentUser;

    private readonly IUserSettingRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSettingAppService(
        IUserSettingRepository repository,
        ICurrentUser currentUser,
        ISaasCacheInvalidator cacheInvalidator)
    {
        _repository = repository;
        _currentUser = currentUser;
        _cacheInvalidator = cacheInvalidator;
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
            _ = await _repository.UpdateAsync(entity, cancellationToken);
        }

        // 写后失效该用户设置读缓存，保证下次读取拿到最新值
        await _cacheInvalidator.InvalidateUserSettingAsync(userId, cancellationToken);

        return new UserSettingDto
        {
            Scene = input.Scene,
            SettingKey = settingKey,
            SettingValue = input.SettingValue
        };
    }
}
