#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionChangeLogRepository
// Guid:a0950a40-3038-4678-bbb9-7c6351cb96b2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 权限变更日志仓储接口
/// </summary>
public interface IPermissionChangeLogRepository : ISaasSplitRepository<SysPermissionChangeLog>
{
}
