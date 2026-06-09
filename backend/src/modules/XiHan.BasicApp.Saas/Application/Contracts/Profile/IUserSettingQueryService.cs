#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSettingQueryService
// Guid:a0612347-8092-4e3f-8b6d-5c701e4f9da6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户设置查询应用服务接口（读侧：按用户 × 场景 × 设置键跨端同步，带分布式缓存）
/// </summary>
public interface IUserSettingQueryService : IApplicationService
{
    /// <summary>
    /// 获取当前用户指定场景与设置键的设置（无则返回空载荷）
    /// </summary>
    Task<UserSettingDto> GetAsync(UserSettingScene scene, string settingKey, CancellationToken cancellationToken = default);
}
