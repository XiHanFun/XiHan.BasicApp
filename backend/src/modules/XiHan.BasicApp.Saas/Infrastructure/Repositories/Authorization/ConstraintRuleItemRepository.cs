#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleItemRepository
// Guid:2757a579-61d6-4f36-9919-c9f0b2d49326
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
/// 约束规则目标项仓储实现
/// </summary>
public sealed class ConstraintRuleItemRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysConstraintRuleItem>(clientResolver), IConstraintRuleItemRepository;
