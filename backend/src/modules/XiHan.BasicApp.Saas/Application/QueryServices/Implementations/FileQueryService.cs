#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileQueryService
// Guid:6e7f8091-0213-2345-ef01-23456789ab02
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices.Implementations;

/// <summary>
/// 文件查询服务
/// </summary>
public class FileQueryService : IFileQueryService, ITransientDependency
{
    private readonly IFileRepository _fileRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileQueryService(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.FileById, ExpireSeconds = 300)]
    public async Task<FileDto?> GetByIdAsync(long id)
    {
        var entity = await _fileRepository.GetByIdAsync(id);
        return entity?.Adapt<FileDto>();
    }
}
