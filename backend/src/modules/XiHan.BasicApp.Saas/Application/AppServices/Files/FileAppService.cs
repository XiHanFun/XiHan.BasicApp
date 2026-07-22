// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统文件命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统文件")]
public sealed class FileAppService
    : SaasApplicationService, IFileAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly IFileDomainService _fileDomainService;

    private readonly IFileRecordQueryService _fileRecordQueryService;

    private readonly IFileTransferService _fileTransferService;

    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileAppService(
        IFileDomainService fileDomainService,
        IFileRecordQueryService fileRecordQueryService,
        IFileTransferService fileTransferService,
        ILocalEventBus localEventBus,
        ICurrentUser currentUser)
    {
        _fileDomainService = fileDomainService;
        _fileRecordQueryService = fileRecordQueryService;
        _fileTransferService = fileTransferService;
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
            await _fileTransferService.DeletePhysicalAsync(storages, cancellationToken);
        }

        var result = await _fileDomainService.DeleteFileAsync(FileApplicationMapper.ToDeleteCommand(input), cancellationToken);

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

        // 计入下载次数（用户发起下载即记一次）
        await _fileDomainService.IncrementDownloadCountAsync(fileId, cancellationToken);

        return await _fileTransferService.DownloadAsync(storage, cancellationToken);
    }

    /// <summary>
    /// 秒传文件（按哈希探测：未命中返回 null，前端回退普通上传）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Create)]
    public async Task<FileDetailDto?> FastUploadFileAsync(FileFastUploadDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fileDomainService.FastUploadFileAsync(FileApplicationMapper.ToFastUploadCommand(input), cancellationToken);
        return result is null ? null : FileApplicationMapper.ToDetailDto(result.File);
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

        // 计入访问次数（生成预签名访问链接即记一次）
        await _fileDomainService.IncrementViewCountAsync(fileId, cancellationToken);

        return await _fileTransferService.GeneratePresignedUrlAsync(storage, expiresIn, cancellationToken);
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
            FileApplicationMapper.ToPrimaryStorageSwitchCommand(input),
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

        var result = await _fileDomainService.UpdateFileMetadataAsync(FileApplicationMapper.ToMetadataUpdateCommand(input), cancellationToken);
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
            FileApplicationMapper.ToStatusCommand(input),
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
            FileApplicationMapper.ToStorageStatusCommand(input),
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
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fileTransferService.UploadAsync(input, cancellationToken);
        if (result.Uploaded && result.Storage is not null)
        {
            await PublishUploadedAsync(result.File, result.Storage, result.Reason, cancellationToken);
        }

        return FileApplicationMapper.ToDetailDto(result.File);
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
        var probe = await _fileTransferService.ProbeStorageAsync(storage, cancellationToken);

        var result = await _fileDomainService.VerifyFileStorageAsync(
            FileApplicationMapper.ToStorageVerifyCommand(input, probe),
            cancellationToken);
        return FileApplicationMapper.ToStorageDetailDto(result.Storage);
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
