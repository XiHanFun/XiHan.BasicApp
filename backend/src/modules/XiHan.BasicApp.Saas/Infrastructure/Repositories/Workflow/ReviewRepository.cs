#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewRepository
// Guid:b4ac7095-a0f1-4ef7-a704-d824a082a875
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
/// 审查仓储实现
/// </summary>
public sealed class ReviewRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysReview>(clientResolver, unitOfWorkManager), IReviewRepository;
