#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PasswordHistoryRepository
// Guid:30a02a51-fd78-4c17-bd97-17f335904e63
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
/// 密码历史仓储实现
/// </summary>
public sealed class PasswordHistoryRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPasswordHistory>(clientResolver), IPasswordHistoryRepository;
