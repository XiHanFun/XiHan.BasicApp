#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthTokenRepository
// Guid:b40c226d-b9f9-4a9d-a475-ea4f08b3a7d9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// OAuth 令牌仓储实现
/// </summary>
public sealed class OAuthTokenRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysOAuthToken>(clientResolver), IOAuthTokenRepository;
