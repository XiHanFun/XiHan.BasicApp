#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserStatisticsRepository
// Guid:ed325b3f-96b4-436f-bfac-18be6bafbc0d
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
/// 用户统计仓储实现
/// </summary>
public sealed class UserStatisticsRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserStatistics>(clientResolver), IUserStatisticsRepository;
