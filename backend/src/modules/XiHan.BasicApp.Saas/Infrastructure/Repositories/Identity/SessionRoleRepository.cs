#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SessionRoleRepository
// Guid:f364f604-11bb-43ca-9ff5-85dc8f931165
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
/// 会话角色仓储实现
/// </summary>
public sealed class SessionRoleRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysSessionRole>(clientResolver), ISessionRoleRepository;
