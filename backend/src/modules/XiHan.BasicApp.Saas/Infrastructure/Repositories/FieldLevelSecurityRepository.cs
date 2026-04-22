#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityRepository
// Guid:89abcdef-0123-4567-89ab-cdef01234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 18:32:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 字段级安全仓储实现
/// </summary>
public class FieldLevelSecurityRepository : SqlSugarAuditedRepository<SysFieldLevelSecurity, long>, IFieldLevelSecurityRepository
{
    public FieldLevelSecurityRepository(ISqlSugarClientResolver clientResolver)
        : base(clientResolver)
    {
    }
}
