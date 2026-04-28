#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewLogRepository
// Guid:079bf425-163f-4bbd-a028-a371c5c941f2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.SplitTables;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 审查日志仓储实现
/// </summary>
public sealed class ReviewLogRepository(
    ISqlSugarClientResolver clientResolver,
    ISplitTableLocator splitTableLocator)
    : SaasSplitRepository<SysReviewLog>(clientResolver, splitTableLocator), IReviewLogRepository;
