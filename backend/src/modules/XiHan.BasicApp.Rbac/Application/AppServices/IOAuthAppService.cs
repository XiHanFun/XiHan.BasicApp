#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthAppService
// Guid:a8e8c8f5-5f5a-4f61-ab39-9f4ed8463f89
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.AppServices;

/// <summary>
/// OAuth应用服务
/// </summary>
public interface IOAuthAppService
    : ICrudApplicationService<OAuthAppDto, long, OAuthAppCreateDto, OAuthAppUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    Task<OAuthAppDto?> GetByClientIdAsync(string clientId, long? tenantId = null);
}
