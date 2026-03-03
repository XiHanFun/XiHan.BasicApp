#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileRepository
// Guid:77a7ef9b-ecc1-4e3f-9701-7cd12d4086cb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 文件仓储接口
/// </summary>
public interface IFileRepository : IAggregateRootRepository<SysFile, long>
{
    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    Task<SysFile?> GetByFileHashAsync(string fileHash, long? tenantId = null, CancellationToken cancellationToken = default);
}
