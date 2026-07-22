// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 文件仓储实现
/// </summary>
public sealed class FileRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysFile>(clientResolver), IFileRepository
{
    /// <inheritdoc />
    public async Task<SysFile?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(file => file.FileHash == fileHash)
            .FirstAsync(cancellationToken);
    }
}
