#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityRepository
// Guid:b58b471f-7b2c-4df6-a2e8-358a949393fa
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
/// 字段级安全仓储实现
/// </summary>
public sealed class FieldLevelSecurityRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysFieldLevelSecurity>(clientResolver), IFieldLevelSecurityRepository;
