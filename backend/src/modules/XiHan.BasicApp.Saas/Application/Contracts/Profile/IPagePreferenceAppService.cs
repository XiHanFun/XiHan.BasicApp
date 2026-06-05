#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPagePreferenceAppService
// Guid:9e501236-7f81-4d2e-9a5c-4b6f0d3e8c95
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 页面偏好应用服务接口（按用户 × 页面码跨端同步列设置/视图）
/// </summary>
public interface IPagePreferenceAppService : IApplicationService
{
    /// <summary>
    /// 获取当前用户指定页面的偏好（无则返回空载荷）
    /// </summary>
    Task<PagePreferenceDto> GetAsync(string pageCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存（upsert）当前用户指定页面的偏好
    /// </summary>
    Task<PagePreferenceDto> SaveAsync(PagePreferenceSaveDto input, CancellationToken cancellationToken = default);
}
