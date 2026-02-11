#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantIsolationMode
// Guid:1a28152c-d6e9-4396-addb-b479254bad90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 09:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 租户隔离模式
/// </summary>
public enum TenantIsolationMode
{
    /// <summary>
    /// 字段隔离 - 通过TenantId字段区分租户数据
    /// </summary>
    Field = 0,

    /// <summary>
    /// 数据库隔离 - 每个租户使用独立数据库
    /// </summary>
    Database = 1,

    /// <summary>
    /// Schema隔离 - 在同一数据库中使用不同Schema
    /// </summary>
    Schema = 2
}
