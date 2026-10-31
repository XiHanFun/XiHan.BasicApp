// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Operations.Files;
using XiHan.BasicApp.Files.Operations.StorageConfigs;
using XiHan.BasicApp.Runtime.Queries;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 4.x 同三个实体：文件、文件的存储副本与存储配置。字段由响应类型生成，规则由 Access 的字段安全中间件在读、建、改处统一套用；
/// 返回文件的上传收尾与存储副本操作也套掩码。
/// </remarks>
public static class FilesFieldSecurity
{
    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("files", FilesJsonContext.Default, fields => fields
            .Entity<FileResponse>("files.files", "files.library.title", "files.library.fields.")
                .Reads(ListFiles.Id, GetFile.Id, CompleteUpload.Id, SetPrimaryStorage.Id, SetStorageStatus.Id, VerifyStorage.Id)
                .Updates<UpdateFileRequest>(UpdateFile.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysFile>(context, request.Id, FileResponse.From))
                .Keyword("originalName", "name", "extension", "contentType", "tags")
            .Entity<FileStorageResponse>("files.file-storages", "files.library.storages_title", "files.library.storage_fields.")
                .ReadsAt(ListFileStorages.Id, "items")
            .Entity<StorageConfigResponse>("files.storage-configs", "files.storage_configs.title", "files.storage_configs.fields.")
                .Reads(ListStorageConfigs.Id, GetStorageConfig.Id)
                .Creates(CreateStorageConfig.Id)
                .Updates<UpdateStorageConfigRequest>(UpdateStorageConfig.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysStorageConfig>(context, request.Id, StorageConfigResponse.From))
                .Keyword("code", "name", "bucket", "remark"));
}
