#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantDatabaseType
// Guid:1a28152c-d6e9-4396-addb-b479254bad90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 09:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 租户数据库类型
/// </summary>
public enum TenantDatabaseType
{
    /// <summary>
    /// SQL Server
    /// </summary>
    SqlServer = 0,

    /// <summary>
    /// MySql
    /// </summary>
    MySql = 1,

    /// <summary>
    /// PostgreSql
    /// </summary>
    PostgreSql = 2,

    /// <summary>
    /// SQLite
    /// </summary>
    SQLite = 3,

    /// <summary>
    /// Oracle
    /// </summary>
    Oracle = 4
}
