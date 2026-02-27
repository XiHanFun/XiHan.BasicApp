#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConfigAppService
// Guid:54f0f5a7-84cb-4ab2-9f0f-a8a370880dda
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:35:35
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 配置应用服务
/// </summary>
public interface IConfigAppService : IApplicationService
{
    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    /// <param name="configKey"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<ConfigDto?> GetConfigByKeyAsync(string configKey, long? tenantId = null);
}
