#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExternalLoginRepository
// Guid:6c915825-f238-4586-abfb-ff9c2e4b5b8e
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
/// 第三方登录绑定仓储实现
/// </summary>
public sealed class ExternalLoginRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysExternalLogin>(clientResolver), IExternalLoginRepository;
