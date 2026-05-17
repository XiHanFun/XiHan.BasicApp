#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileAppService
// Guid:0de54b67-1c8c-4b25-af1c-b010c60f2091
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.ObjectStorage;
using XiHan.Framework.ObjectStorage.Models;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统文件命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统文件")]
public sealed class FileAppService
    : SaasApplicationService, IFileAppService
{
    private const string Sha256AlgorithmName = "SHA256";

    private readonly IClientInfoProvider _clientInfoProvider;

    private readonly ICurrentUser _currentUser;

    private readonly IFileDomainService _fileDomainService;

    private readonly IFileRecordQueryService _fileRecordQueryService;

    private readonly IFileStorageDomainService _fileStorageDomainService;

    private readonly IFileStorageProviderManager _fileStorageProviderManager;

    private readonly IFileStorageRouter _fileStorageRouter;

    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileAppService(
        IFileDomainService fileDomainService,
        IFileRecordQueryService fileRecordQueryService,
        IFileStorageRouter fileStorageRouter,
        IFileStorageProviderManager fileStorageProviderManager,
        IFileStorageDomainService fileStorageDomainService,
        IClientInfoProvider clientInfoProvider,
        ILocalEventBus localEventBus,
        ICurrentUser currentUser)
    {
        _fileDomainService = fileDomainService;
        _fileRecordQueryService = fileRecordQueryService;
        _fileStorageRouter = fileStorageRouter;
        _fileStorageProviderManager = fileStorageProviderManager;
        _fileStorageDomainService = fileStorageDomainService;
        _clientInfoProvider = clientInfoProvider;
        _localEventBus = localEventBus;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Delete)]
    public async Task DeleteFileAsync(FileDeleteDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var file = await _fileRecordQueryService.GetFileOrThrowAsync(input.BasicId, cancellationToken);
        var storages = await _fileRecordQueryService.GetStoragesByFileIdAsync(file.BasicId, cancellationToken);

        if (input.DeletePhysical)
        {
            foreach (var storage in storages)
            {
                var provider = _fileStorageRouter.Route(providerName: storage.StorageProvider);
                await provider.DeleteAsync(storage.StoragePath, storage.BucketName, cancellationToken);
            }
        }

        var result = await _fileDomainService.DeleteFileAsync(new FileDeleteCommand(input.BasicId, input.Reason), cancellationToken);

        await _localEventBus.PublishAsync(
            new FileDeletedDomainEvent(
                result.File.TenantId,
                result.File.BasicId,
                result.File.FileName,
                input.DeletePhysical,
                _currentUser.UserId,
                input.Reason));
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    [UnitOfWork]
    [PermissionAuthorize(SaasPermissionCodes.File.Read)]
    public async Task<Stream> DownloadFileAsync(long fileId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storage = await _fileRecordQueryService.GetPrimaryStorageOrThrowAsync(fileId, "文件缺少可用主存储，无法下载。", cancellationToken);

        var provider = _fileStorageRouter.Route(providerName: storage.StorageProvider);
        return await provider.DownloadAsync(storage.StoragePath, cancellationToken);
    }

    /// <summary>
    /// 秒传文件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Create)]
    public async Task<FileDetailDto> FastUploadFileAsync(FileFastUploadDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fileDomainService.FastUploadFileAsync(ToFastUploadCommand(input), cancellationToken);
        return FileApplicationMapper.ToDetailDto(result.File);
    }

    /// <summary>
    /// 生成文件预签名访问 URL
    /// </summary>
    [UnitOfWork]
    [PermissionAuthorize(SaasPermissionCodes.File.Read)]
    public async Task<string> GenerateFilePresignedUrlAsync(long fileId, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storage = await _fileRecordQueryService.GetPrimaryStorageOrThrowAsync(fileId, "文件缺少可用主存储，无法生成访问链接。", cancellationToken);

        var provider = _fileStorageRouter.Route(providerName: storage.StorageProvider);
        var effectiveExpiresIn = expiresIn ?? TimeSpan.FromMinutes(30);
        return await provider.GeneratePresignedUrlAsync(storage.StoragePath, effectiveExpiresIn, cancellationToken);
    }

    /// <summary>
    /// 切换主存储
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Update)]
    public async Task<FileStorageDetailDto> SwitchPrimaryStorageAsync(FilePrimaryStorageSwitchDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fileDomainService.SwitchPrimaryStorageAsync(
            new FilePrimaryStorageSwitchCommand(input.BasicId, input.StorageId, input.Remark),
            cancellationToken);

        await _localEventBus.PublishAsync(
            new FilePrimaryStorageChangedDomainEvent(
                result.Storage.TenantId,
                result.Storage.FileId,
                result.Storage.BasicId,
                result.PreviousStorageId,
                _currentUser.UserId,
                input.Remark));

        return FileApplicationMapper.ToStorageDetailDto(result.Storage);
    }

    /// <summary>
    /// 更新文件业务元数据
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Update)]
    public async Task<FileDetailDto> UpdateFileMetadataAsync(FileMetadataUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fileDomainService.UpdateFileMetadataAsync(ToMetadataUpdateCommand(input), cancellationToken);
        return FileApplicationMapper.ToDetailDto(result.File);
    }

    /// <summary>
    /// 更新系统文件状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Status)]
    public async Task<FileDetailDto> UpdateFileStatusAsync(FileStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fileDomainService.UpdateFileStatusAsync(
            new FileStatusUpdateCommand(input.BasicId, input.Status, input.Remark),
            cancellationToken);
        return FileApplicationMapper.ToDetailDto(result.File);
    }

    /// <summary>
    /// 更新系统文件存储状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Status)]
    public async Task<FileStorageDetailDto> UpdateFileStorageStatusAsync(FileStorageStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fileDomainService.UpdateFileStorageStatusAsync(
            new FileStorageStatusUpdateCommand(input.BasicId, input.Status, input.UploadFailureReason, input.Remark),
            cancellationToken);
        return FileApplicationMapper.ToStorageDetailDto(result.Storage);
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Create)]
    public async Task<FileDetailDto> UploadFileAsync(FileUploadDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.File);
        cancellationToken.ThrowIfCancellationRequested();

        var originalName = _fileStorageDomainService.NormalizeOriginalName(input.File.FileName);
        var fileHash = await ComputeSha256Async(input.File, cancellationToken);
        var existing = await _fileRecordQueryService.GetNormalFileByHashAsync(fileHash, cancellationToken);
        if (existing is not null)
        {
            return FileApplicationMapper.ToDetailDto(existing);
        }

        var now = DateTimeOffset.UtcNow;
        var resolvedProviderName = _fileStorageRouter.ResolveProviderName(input.RouteKey, input.ProviderName);
        var provider = _fileStorageRouter.Route(input.RouteKey, resolvedProviderName);
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
                input.ExpiresAt,
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
        await PublishUploadedAsync(uploadCommandResult.File, uploadCommandResult.Storage, input.Remark, cancellationToken);
        return FileApplicationMapper.ToDetailDto(uploadCommandResult.File);
    }

    /// <summary>
    /// 校验文件存储副本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Status)]
    public async Task<FileStorageDetailDto> VerifyFileStorageAsync(FileStorageVerifyDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var storage = await _fileRecordQueryService.GetStorageOrThrowAsync(input.BasicId, cancellationToken);
        var provider = _fileStorageRouter.Route(providerName: storage.StorageProvider);
        var exists = await provider.ExistsAsync(storage.StoragePath, storage.BucketName, cancellationToken);
        FileMetadata? metadata = null;

        if (exists)
        {
            metadata = await provider.GetMetadataAsync(storage.StoragePath, storage.BucketName, cancellationToken);
        }

        var result = await _fileDomainService.VerifyFileStorageAsync(
            new FileStorageVerifyCommand(input.BasicId, exists, metadata?.Size ?? 0, metadata?.Url, input.Remark),
            cancellationToken);
        return FileApplicationMapper.ToStorageDetailDto(result.Storage);
    }

    private static FileFastUploadCommand ToFastUploadCommand(FileFastUploadDto input)
    {
        return new FileFastUploadCommand(
            input.FileHash,
            input.OriginalName,
            input.FileSize,
            input.MimeType,
            input.FileExtension,
            input.AccessLevel,
            input.AccessPermissions,
            input.ExpiresAt,
            input.IsTemporary,
            input.RetentionDays,
            input.Tags,
            input.Remark,
            input.ExtendData);
    }

    private static FileMetadataUpdateCommand ToMetadataUpdateCommand(FileMetadataUpdateDto input)
    {
        return new FileMetadataUpdateCommand(
            input.BasicId,
            input.Width,
            input.Height,
            input.Duration,
            input.ThumbnailFileId,
            input.AccessLevel,
            input.AccessPermissions,
            input.IsEncrypted,
            input.EncryptionKeyId,
            input.ExpiresAt,
            input.IsTemporary,
            input.RetentionDays,
            input.Tags,
            input.Remark,
            input.ExtendData);
    }

    private static async Task<string> ComputeSha256Async(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken)
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

    private async Task PublishUploadedAsync(SysFile file, SysFileStorage storage, string? reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _localEventBus.PublishAsync(
            new FileUploadedDomainEvent(
                file.TenantId,
                file.BasicId,
                storage.BasicId,
                file.FileName,
                file.FileSize,
                _currentUser.UserId,
                reason));
    }
}
