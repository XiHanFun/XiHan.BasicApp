// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.ObjectStorage;
using XiHan.Framework.ObjectStorage.Models;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 文件传输服务实现
/// </summary>
public sealed class FileTransferService
    : IFileTransferService
{
    private const string Sha256AlgorithmName = "SHA256";

    private readonly IClientInfoProvider _clientInfoProvider;

    private readonly IFileDomainService _fileDomainService;

    private readonly IFileRecordQueryService _fileRecordQueryService;

    private readonly IFileStorageDomainService _fileStorageDomainService;

    private readonly IStorageProviderResolver _storageProviderResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileTransferService(
        IFileDomainService fileDomainService,
        IFileRecordQueryService fileRecordQueryService,
        IStorageProviderResolver storageProviderResolver,
        IFileStorageDomainService fileStorageDomainService,
        IClientInfoProvider clientInfoProvider)
    {
        _fileDomainService = fileDomainService;
        _fileRecordQueryService = fileRecordQueryService;
        _storageProviderResolver = storageProviderResolver;
        _fileStorageDomainService = fileStorageDomainService;
        _clientInfoProvider = clientInfoProvider;
    }

    /// <inheritdoc />
    public async Task DeletePhysicalAsync(IReadOnlyList<SysFileStorage> storages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storages);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var storage in storages)
        {
            var provider = await _storageProviderResolver.RouteForProviderAsync(storage.StorageProvider, cancellationToken);
            await provider.DeleteAsync(storage.StoragePath, storage.BucketName, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(SysFileStorage storage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        cancellationToken.ThrowIfCancellationRequested();

        var provider = await _storageProviderResolver.RouteForProviderAsync(storage.StorageProvider, cancellationToken);
        return await provider.DownloadAsync(storage.StoragePath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GeneratePresignedUrlAsync(SysFileStorage storage, TimeSpan? expiresIn, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        cancellationToken.ThrowIfCancellationRequested();

        var provider = await _storageProviderResolver.RouteForProviderAsync(storage.StorageProvider, cancellationToken);
        return await provider.GeneratePresignedUrlAsync(storage.StoragePath, expiresIn ?? TimeSpan.FromMinutes(30), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileStorageProbeResult> ProbeStorageAsync(SysFileStorage storage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        cancellationToken.ThrowIfCancellationRequested();

        var provider = await _storageProviderResolver.RouteForProviderAsync(storage.StorageProvider, cancellationToken);
        var exists = await provider.ExistsAsync(storage.StoragePath, storage.BucketName, cancellationToken);
        FileMetadata? metadata = null;

        if (exists)
        {
            metadata = await provider.GetMetadataAsync(storage.StoragePath, storage.BucketName, cancellationToken);
        }

        return new FileStorageProbeResult(exists, metadata?.Size ?? 0, metadata?.Url);
    }

    /// <inheritdoc />
    public async Task<FileTransferUploadResult> UploadAsync(FileUploadDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.File);
        cancellationToken.ThrowIfCancellationRequested();

        var originalName = _fileStorageDomainService.NormalizeOriginalName(input.File.FileName);
        var fileHash = await ComputeSha256Async(input.File, cancellationToken);
        var existing = await _fileRecordQueryService.GetNormalFileByHashAsync(fileHash, cancellationToken);
        if (existing is not null)
        {
            return new FileTransferUploadResult(existing, Storage: null, Uploaded: false, Reason: input.Remark);
        }

        var now = DateTimeOffset.UtcNow;
        var resolvedProviderName = _storageProviderResolver.ResolveProviderName(input.RouteKey, input.ProviderName);
        var provider = await _storageProviderResolver.RouteForUploadAsync(input.RouteKey, resolvedProviderName, cancellationToken);
        var storedFileName = _fileStorageDomainService.BuildStoredFileName(originalName, fileHash);
        var storagePath = _fileStorageDomainService.BuildStoragePath(storedFileName, input.Directory, now);
        var accessControl = _fileStorageDomainService.ResolveAccessControl(input.AccessLevel, input.AccessControl);
        var clientInfo = _clientInfoProvider.GetCurrent();

        var fileResult = await _fileDomainService.CreateUploadingFileAsync(
            new FileCreateUploadingCommand(
                storedFileName,
                originalName,
                Path.GetExtension(originalName),
                _fileStorageDomainService.ResolveFileType(Path.GetExtension(originalName), input.File.ContentType),
                input.File.ContentType,
                input.File.Length,
                fileHash,
                Sha256AlgorithmName,
                input.Width,
                input.Height,
                input.Duration,
                input.ThumbnailFileId,
                clientInfo.IpAddress,
                ResolveUploadSource(clientInfo),
                input.AccessLevel,
                input.AccessPermissions,
                input.IsEncrypted,
                input.EncryptionKeyId,
                input.ExpirationTime,
                input.IsTemporary,
                input.RetentionDays,
                input.Tags,
                input.Remark,
                input.ExtendData),
            cancellationToken);
        var file = fileResult.File;

        FileUploadResult uploadResult;
        await using (var stream = input.File.OpenReadStream())
        {
            uploadResult = await provider.UploadAsync(
                new FileUploadRequest
                {
                    FileStream = stream,
                    FileName = storedFileName,
                    StoragePath = storagePath,
                    ContentType = input.File.ContentType,
                    BucketName = NormalizeText(input.BucketName, 100),
                    Overwrite = input.Overwrite,
                    AccessControl = accessControl,
                    CacheControl = NormalizeText(input.CacheControl, 100),
                    Metadata = new Dictionary<string, string>
                    {
                        ["file-id"] = file.BasicId.ToString(),
                        ["file-hash"] = fileHash,
                        ["hash-algorithm"] = Sha256AlgorithmName
                    }
                },
                cancellationToken);
        }

        if (!uploadResult.Success)
        {
            _ = await _fileDomainService.MarkUploadFailedAsync(new FileUploadFailedCommand(file.BasicId, uploadResult.ErrorMessage), cancellationToken);
            throw new InvalidOperationException(uploadResult.ErrorMessage ?? "文件上传失败。");
        }

        var uploadCommandResult = await _fileDomainService.CompleteUploadAsync(
            new FileUploadCompleteCommand(
                file.BasicId,
                uploadResult.FileSize,
                _fileStorageDomainService.ResolveStorageType(provider.ProviderName),
                provider.ProviderName,
                input.BucketName,
                uploadResult.Path ?? storagePath,
                uploadResult.FullPath,
                uploadResult.Url,
                uploadResult.Url,
                now,
                uploadResult.DurationMs,
                accessControl,
                input.CacheControl,
                input.Remark),
            cancellationToken);

        return new FileTransferUploadResult(uploadCommandResult.File, uploadCommandResult.Storage, Uploaded: true, input.Remark);
    }

    private static async Task<string> ComputeSha256Async(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? ResolveUploadSource(ClientInfo clientInfo)
    {
        var parts = new[]
        {
            NormalizeText(clientInfo.DeviceName, 20),
            NormalizeText(clientInfo.OperatingSystem, 20),
            NormalizeText(clientInfo.Browser, 20)
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        return NormalizeText(string.Join("/", parts), 50);
    }
}
