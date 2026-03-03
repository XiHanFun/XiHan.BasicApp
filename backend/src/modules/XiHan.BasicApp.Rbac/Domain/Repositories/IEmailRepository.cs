#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IEmailRepository
// Guid:8f726f9d-dfc9-48d4-ae53-37f38f7739a8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:08:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 邮件仓储接口
/// </summary>
public interface IEmailRepository : IAggregateRootRepository<SysEmail, long>
{
    /// <summary>
    /// 获取待发送邮件
    /// </summary>
    Task<IReadOnlyList<SysEmail>> GetPendingEmailsAsync(int maxCount = 100, long? tenantId = null, CancellationToken cancellationToken = default);
}
