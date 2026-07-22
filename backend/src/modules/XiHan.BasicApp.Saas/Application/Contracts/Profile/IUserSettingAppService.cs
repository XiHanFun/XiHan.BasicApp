// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户设置应用服务接口（写侧：按用户 × 场景 × 设置键 upsert）
/// </summary>
public interface IUserSettingAppService : IApplicationService
{
    /// <summary>
    /// 保存（upsert）当前用户指定场景与设置键的设置
    /// </summary>
    Task<UserSettingDto> SaveAsync(UserSettingSaveDto input, CancellationToken cancellationToken = default);
}
