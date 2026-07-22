// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 文件记录查询服务实现
/// </summary>
public sealed class FileRecordQueryService
    : IFileRecordQueryService
{
    private readonly IFileRepository _fileRepository;

    private readonly IFileStorageRepository _fileStorageRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileRecordQueryService(IFileRepository fileRepository, IFileStorageRepository fileStorageRepository)
    {
        _fileRepository = fileRepository;
        _fileStorageRepository = fileStorageRepository;
    }

    /// <inheritdoc />
    public async Task<SysFile> GetFileOrThrowAsync(long id, CancellationToken cancellationToken = default)
    {
        EnsureId(id, "系统文件主键必须大于 0。");
        return await _fileRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统文件不存在。");
    }

    /// <inheritdoc />
    public async Task<SysFile?> GetNormalFileByHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        var file = await _fileRepository.GetByHashAsync(fileHash.Trim(), cancellationToken);
        return file?.Status == FileStatus.Normal ? file : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysFileStorage>> GetStoragesByFileIdAsync(long fileId, CancellationToken cancellationToken = default)
    {
        EnsureId(fileId, "系统文件主键必须大于 0。");
        return await _fileStorageRepository.GetByFileIdAsync(fileId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysFileStorage> GetPrimaryStorageOrThrowAsync(long fileId, string errorMessage, CancellationToken cancellationToken = default)
    {
        EnsureId(fileId, "系统文件主键必须大于 0。");
        return await _fileStorageRepository.GetPrimaryByFileIdAsync(fileId, cancellationToken)
            ?? throw new InvalidOperationException(errorMessage);
    }

    /// <inheritdoc />
    public async Task<SysFileStorage> GetStorageOrThrowAsync(long id, CancellationToken cancellationToken = default)
    {
        EnsureId(id, "系统文件存储主键必须大于 0。");
        return await _fileStorageRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统文件存储不存在。");
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }
}
