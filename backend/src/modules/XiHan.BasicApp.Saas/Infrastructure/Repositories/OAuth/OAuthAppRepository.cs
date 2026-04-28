#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppRepository
// Guid:63adf6a6-0245-4b8a-819f-41087b9636c0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// OAuth 应用仓储实现
/// </summary>
public sealed class OAuthAppRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysOAuthApp>(clientResolver, unitOfWorkManager), IOAuthAppRepository;
