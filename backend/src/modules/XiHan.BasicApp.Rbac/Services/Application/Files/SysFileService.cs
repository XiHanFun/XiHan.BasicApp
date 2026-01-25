#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFileService
// Guid:d6e7f8a9-b0c1-4f2d-e3f4-a5b6c7d8e9f0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/10 10:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.BasicApp.Rbac.Services.Application.Files.Dtos;
using XiHan.Framework.ObjectStorage;
using XiHan.Framework.ObjectStorage.Models;
using XiHan.Framework.VirtualFileSystem.Processing;

namespace XiHan.BasicApp.Rbac.Services.Application.Files;

/// <summary>
/// 文件上传服务
/// </summary>
public class SysFileService : /*CrudApplicationServiceBase<>,*/ IFileUploadService
{
    private readonly ILogger<SysFileService> _logger;
    private readonly IFileStorageProvider _storageProvider;
    private readonly IImageProcessingService? _imageProcessingService;
    private readonly IVideoProcessingService? _videoProcessingService;
    private readonly IFileRepository _fileRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysFileService(
        ILogger<SysFileService> logger,
        IFileStorageProvider storageProvider,
        IFileRepository fileRepository,
        IImageProcessingService? imageProcessingService = null,
        IVideoProcessingService? videoProcessingService = null)
    {
        _logger = logger;
        _storageProvider = storageProvider;
        _fileRepository = fileRepository;
        _imageProcessingService = imageProcessingService;
        _videoProcessingService = videoProcessingService;
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    public async Task<SysFile> UploadAsync(FileUploadDto fileUploadDto, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 计算文件哈希（用于去重）
            var fileHash = await ComputeFileHashAsync(fileUploadDto.FileStream, cancellationToken);
            fileUploadDto.FileStream.Position = 0;

            // 2. 检查是否已存在相同文件（可选去重）
            if (fileUploadDto.EnableDeduplication)
            {
                var existingFile = await _fileRepository.GetByFileHashAsync(fileHash, cancellationToken);
                if (existingFile != null)
                {
                    _logger.LogInformation("File already exists with hash {Hash}, reusing", fileHash);
                    return existingFile;
                }
            }

            // 3. 生成存储路径
            var storagePath = GenerateStoragePath(fileUploadDto.FileName);

            // 4. 上传到存储提供商
            FileUploadResult uploadResult;
            if (fileUploadDto.EnableChunkedUpload && fileUploadDto.FileSize > fileUploadDto.ChunkThreshold)
            {
                uploadResult = await UploadChunkedAsync(fileUploadDto, storagePath, cancellationToken);
            }
            else
            {
                uploadResult = await _storageProvider.UploadAsync(new FileUploadRequest
                {
                    FileStream = fileUploadDto.FileStream,
                    FileName = fileUploadDto.FileName,
                    StoragePath = storagePath,
                    ContentType = fileUploadDto.ContentType,
                    BucketName = fileUploadDto.BucketName,
                    Overwrite = false,
                    AccessControl = fileUploadDto.IsPublic ? "public-read" : "private",
                    ProgressCallback = fileUploadDto.ProgressCallback
                }, cancellationToken);
            }

            if (!uploadResult.Success)
            {
                throw new InvalidOperationException($"Upload failed: {uploadResult.ErrorMessage}");
            }

            // 5. 创建文件实体
            var file = await CreateFileEntityAsync(fileUploadDto, uploadResult, fileHash, cancellationToken);

            // 6. 创建存储记录
            var storage = await CreateStorageEntityAsync(file, uploadResult, fileUploadDto, cancellationToken);

            // 7. 处理图片/视频
            await PostProcessFileAsync(file, storage, fileUploadDto, cancellationToken);

            _logger.LogInformation("File uploaded successfully: {FileName}", fileUploadDto.FileName);

            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileUploadDto.FileName);
            throw;
        }
    }

    /// <summary>
    /// 分片上传
    /// </summary>
    private async Task<FileUploadResult> UploadChunkedAsync(
        FileUploadDto fileUploadDto,
        string storagePath,
        CancellationToken cancellationToken)
    {
        // 1. 初始化分片上传
        var uploadId = await _storageProvider.InitiateChunkedUploadAsync(new ChunkedUploadInitRequest
        {
            FileName = fileUploadDto.FileName,
            StoragePath = storagePath,
            TotalSize = fileUploadDto.FileSize,
            ChunkSize = fileUploadDto.ChunkSize,
            ContentType = fileUploadDto.ContentType,
            BucketName = fileUploadDto.BucketName
        }, cancellationToken);

        // 2. 上传各个分片
        var chunkInfos = new List<ChunkInfo>();
        var totalChunks = (int)Math.Ceiling((double)fileUploadDto.FileSize / fileUploadDto.ChunkSize);
        var buffer = new byte[fileUploadDto.ChunkSize];

        for (var chunkNumber = 1; chunkNumber <= totalChunks; chunkNumber++)
        {
            var bytesRead = await fileUploadDto.FileStream.ReadAsync(buffer, 0, fileUploadDto.ChunkSize, cancellationToken);
            var chunkStream = new MemoryStream(buffer, 0, bytesRead);

            var chunkResult = await _storageProvider.UploadChunkAsync(new ChunkUploadRequest
            {
                UploadId = uploadId,
                ChunkNumber = chunkNumber,
                ChunkData = chunkStream,
                ChunkSize = bytesRead,
                TotalSize = fileUploadDto.FileSize,
                TotalChunks = totalChunks,
                StoragePath = storagePath,
            }, cancellationToken);

            if (!chunkResult.Success)
            {
                await _storageProvider.AbortChunkedUploadAsync(uploadId, cancellationToken);
                throw new InvalidOperationException($"Chunk {chunkNumber} upload failed: {chunkResult.ErrorMessage}");
            }

            chunkInfos.Add(new ChunkInfo
            {
                ChunkNumber = chunkNumber,
                ETag = chunkResult.ETag
            });

            fileUploadDto.ProgressCallback?.Invoke(chunkNumber * fileUploadDto.ChunkSize, fileUploadDto.FileSize);
        }

        // 3. 完成分片上传
        return await _storageProvider.CompleteChunkedUploadAsync(new ChunkedUploadCompleteRequest
        {
            UploadId = uploadId,
            StoragePath = storagePath,
            ChunkInfos = chunkInfos,
            BucketName = fileUploadDto.BucketName
        }, cancellationToken);
    }

    /// <summary>
    /// 创建文件实体
    /// </summary>
    private async Task<SysFile> CreateFileEntityAsync(
        FileUploadDto fileUploadDto,
        FileUploadResult uploadResult,
        string fileHash,
        CancellationToken cancellationToken)
    {
        var file = new SysFile
        {
            TenantId = fileUploadDto.TenantId,
            FileName = GenerateUniqueFileName(fileUploadDto.FileName),
            OriginalName = fileUploadDto.FileName,
            FileExtension = Path.GetExtension(fileUploadDto.FileName),
            FileType = DetermineFileType(fileUploadDto.FileName),
            MimeType = fileUploadDto.ContentType,
            FileSize = fileUploadDto.FileSize,
            FileHash = fileHash,
            HashAlgorithm = "MD5",
            UploadIp = fileUploadDto.UploadIp,
            UploadSource = fileUploadDto.UploadSource,
            IsPublic = fileUploadDto.IsPublic,
            RequireAuth = !fileUploadDto.IsPublic,
            IsTemporary = fileUploadDto.IsTemporary,
            Status = FileStatus.Normal
        };

        if (fileUploadDto.Tags != null && fileUploadDto.Tags.Any())
        {
            file.Tags = string.Join(",", fileUploadDto.Tags);
        }

        //await _fileRepository.InsertAsync(file, cancellationToken);

        return file;
    }

    /// <summary>
    /// 创建存储实体
    /// </summary>
    private async Task<SysFileStorage> CreateStorageEntityAsync(
        SysFile file,
        FileUploadResult uploadResult,
        FileUploadDto fileUploadDto,
        CancellationToken cancellationToken)
    {
        var storage = new SysFileStorage
        {
            FileId = file.BasicId,
            StorageType = MapStorageType(_storageProvider.ProviderName),
            StorageProvider = _storageProvider.ProviderName,
            BucketName = fileUploadDto.BucketName,
            StoragePath = uploadResult.Path!,
            FullPath = uploadResult.FullPath,
            ExternalUrl = uploadResult.Url,
            IsPrimary = true,
            EnableCdn = fileUploadDto.EnableCdn,
            Status = StorageStatus.Normal,
            UploadedAt = DateTimeOffset.Now,
            UploadDuration = uploadResult.DurationMs,
            IsVerified = true,
            LastVerifiedAt = DateTimeOffset.Now,
            IsSynced = true,
            SyncedAt = DateTimeOffset.Now
        };

        //await _fileStorageRepository.InsertAsync(storage, cancellationToken);

        return storage;
    }

    /// <summary>
    /// 后处理文件（生成缩略图、提取视频封面等）
    /// </summary>
    private async Task PostProcessFileAsync(
        SysFile file,
        SysFileStorage storage,
        FileUploadDto fileUploadDto,
        CancellationToken cancellationToken)
    {
        // 图片后处理
        if (file.FileType == FileType.Image && _imageProcessingService != null)
        {
            await ProcessImageAsync(file, storage, fileUploadDto, cancellationToken);
        }

        // 视频后处理
        if (file.FileType == FileType.Video && _videoProcessingService != null)
        {
            await ProcessVideoAsync(file, storage, fileUploadDto, cancellationToken);
        }
    }

    /// <summary>
    /// 处理图片
    /// </summary>
    private async Task ProcessImageAsync(
        SysFile file,
        SysFileStorage storage,
        FileUploadDto fileUploadDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // 获取图片信息
            var imageStream = await _storageProvider.DownloadAsync(storage.StoragePath, cancellationToken);
            var imageInfo = await _imageProcessingService!.GetImageInfoAsync(imageStream, cancellationToken);

            file.Width = imageInfo.Width;
            file.Height = imageInfo.Height;

            // 生成缩略图（如果需要）
            if (fileUploadDto.GenerateThumbnail)
            {
                await GenerateThumbnailAsync(file, storage, cancellationToken);
            }

            await _fileRepository.UpdateAsync(file, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing image: {FileName}", file.FileName);
        }
    }

    /// <summary>
    /// 处理视频
    /// </summary>
    private async Task ProcessVideoAsync(
        SysFile file,
        SysFileStorage storage,
        FileUploadDto fileUploadDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // 获取视频信息（需要本地文件路径）
            if (!string.IsNullOrEmpty(storage.FullPath))
            {
                var videoInfo = await _videoProcessingService!.GetVideoInfoAsync(storage.FullPath, cancellationToken);

                file.Width = videoInfo.Width;
                file.Height = videoInfo.Height;
                file.Duration = (int)videoInfo.Duration;

                await _fileRepository.UpdateAsync(file, cancellationToken);
            }

            // 提取封面（如果需要）
            if (fileUploadDto.ExtractVideoCover)
            {
                await ExtractVideoCoverAsync(file, storage, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing video: {FileName}", file.FileName);
        }
    }

    /// <summary>
    /// 生成缩略图
    /// </summary>
    private async Task GenerateThumbnailAsync(SysFile file, SysFileStorage storage, CancellationToken cancellationToken)
    {
        // 实现缩略图生成逻辑
        await Task.CompletedTask;
    }

    /// <summary>
    /// 提取视频封面
    /// </summary>
    private async Task ExtractVideoCoverAsync(SysFile file, SysFileStorage storage, CancellationToken cancellationToken)
    {
        // 实现视频封面提取逻辑
        await Task.CompletedTask;
    }

    #region 辅助方法

    /// <summary>
    /// 计算文件哈希
    /// </summary>
    private async Task<string> ComputeFileHashAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = await md5.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 生成存储路径
    /// </summary>
    private string GenerateStoragePath(string fileName)
    {
        var now = DateTimeOffset.Now;
        var directory = "files";
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";

        return $"{directory}/{now:yyyy/MM/dd}/{uniqueFileName}";
    }

    /// <summary>
    /// 生成唯一文件名
    /// </summary>
    private string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        return $"{Guid.NewGuid():N}{extension}";
    }

    /// <summary>
    /// 判断文件类型
    /// </summary>
    private FileType DetermineFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => FileType.Image,
            ".mp4" or ".avi" or ".mov" or ".mkv" or ".flv" or ".wmv" => FileType.Video,
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => FileType.Audio,
            ".doc" or ".docx" or ".pdf" or ".txt" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => FileType.Document,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => FileType.Archive,
            _ => FileType.Other
        };
    }

    /// <summary>
    /// 映射存储类型
    /// </summary>
    private StorageType MapStorageType(string providerName)
    {
        return providerName switch
        {
            "Local" => StorageType.Local,
            "MinIO" => StorageType.Minio,
            "AliyunOSS" => StorageType.AliyunOss,
            "TencentCOS" => StorageType.TencentCos,
            _ => StorageType.Local
        };
    }

    #endregion
}
