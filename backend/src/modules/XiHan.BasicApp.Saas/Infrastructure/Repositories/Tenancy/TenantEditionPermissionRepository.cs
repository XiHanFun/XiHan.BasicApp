#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionPermissionRepository
// Guid:d26aa51b-580f-4fbb-b60c-3139405bc3f3
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
/// 租户版本权限仓储实现
/// </summary>
public sealed class TenantEditionPermissionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysTenantEditionPermission>(clientResolver), ITenantEditionPermissionRepository;
