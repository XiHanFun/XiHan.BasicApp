// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 文件仓储接口
/// </summary>
public interface IFileRepository : ISaasRepository<SysFile>
{
    /// <summary>
    /// 根据文件哈希获取
    /// </summary>
    Task<SysFile?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default);
}
