#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSettingAppService
// Guid:b1723458-9103-4f50-9c7e-6d812f50aeb7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
