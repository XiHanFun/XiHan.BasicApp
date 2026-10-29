// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件包的消息键。
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（<c>-</c> 换成 <c>_</c>）。
/// </remarks>
public static class FilesMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.files";

    /// <summary>
    /// 操作 <c>files.list</c> 的说明。
    /// </summary>
    public const string ListFilesDescription = "files.list.description";

    /// <summary>
    /// 操作 <c>files.get</c> 的说明。
    /// </summary>
    public const string GetFileDescription = "files.get.description";

    /// <summary>
    /// 操作 <c>files.update</c> 的说明。
    /// </summary>
    public const string UpdateFileDescription = "files.update.description";

    /// <summary>
    /// 操作 <c>files.set-status</c> 的说明。
    /// </summary>
    public const string SetFileStatusDescription = "files.set_status.description";

    /// <summary>
    /// 操作 <c>files.delete</c> 的说明。
    /// </summary>
    public const string DeleteFilesDescription = "files.delete.description";

    /// <summary>
    /// 操作 <c>files.presign</c> 的说明。
    /// </summary>
    public const string PresignFileDescription = "files.presign.description";

    /// <summary>
    /// 操作 <c>files.fast-upload</c> 的说明。
    /// </summary>
    public const string FastUploadFileDescription = "files.fast_upload.description";

    /// <summary>
    /// 操作 <c>files.storages</c> 的说明。
    /// </summary>
    public const string ListFileStoragesDescription = "files.storages.description";

    /// <summary>
    /// 操作 <c>files.set-primary-storage</c> 的说明。
    /// </summary>
    public const string SetPrimaryStorageDescription = "files.set_primary_storage.description";

    /// <summary>
    /// 操作 <c>files.verify-storage</c> 的说明。
    /// </summary>
    public const string VerifyStorageDescription = "files.verify_storage.description";

    /// <summary>
    /// 操作 <c>files.set-storage-status</c> 的说明。
    /// </summary>
    public const string SetStorageStatusDescription = "files.set_storage_status.description";

    /// <summary>
    /// 操作 <c>storage-configs.list</c> 的说明。
    /// </summary>
    public const string ListStorageConfigsDescription = "storage_configs.list.description";

    /// <summary>
    /// 操作 <c>storage-configs.get</c> 的说明。
    /// </summary>
    public const string GetStorageConfigDescription = "storage_configs.get.description";

    /// <summary>
    /// 操作 <c>storage-configs.create</c> 的说明。
    /// </summary>
    public const string CreateStorageConfigDescription = "storage_configs.create.description";

    /// <summary>
    /// 操作 <c>storage-configs.update</c> 的说明。
    /// </summary>
    public const string UpdateStorageConfigDescription = "storage_configs.update.description";

    /// <summary>
    /// 操作 <c>storage-configs.set-status</c> 的说明。
    /// </summary>
    public const string SetStorageConfigStatusDescription = "storage_configs.set_status.description";

    /// <summary>
    /// 操作 <c>storage-configs.set-default</c> 的说明。
    /// </summary>
    public const string SetDefaultStorageConfigDescription = "storage_configs.set_default.description";

    /// <summary>
    /// 操作 <c>storage-configs.delete</c> 的说明。
    /// </summary>
    public const string DeleteStorageConfigsDescription = "storage_configs.delete.description";

    /// <summary>
    /// 文件不存在（或不在当前一侧）。
    /// </summary>
    public const string FileNotFound = "files.file.not_found";

    /// <summary>
    /// 存储配置不存在（或不在当前一侧）。
    /// </summary>
    public const string StorageConfigNotFound = "files.storage_config.not_found";

    /// <summary>
    /// 副本不存在。
    /// </summary>
    public const string FileStorageNotFound = "files.file_storage.not_found";

    /// <summary>
    /// 签名下载地址无效或已过期。
    /// </summary>
    public const string LinkInvalid = "files.link.invalid";

    /// <summary>
    /// 存储配置编码为空或含空白。
    /// </summary>
    public const string StorageCodeInvalid = "files.storage_config.code_invalid";

    /// <summary>
    /// 桶名不合规则。
    /// </summary>
    public const string BucketInvalid = "files.storage_config.bucket_invalid";

    /// <summary>
    /// S3 兼容存储缺服务地址或访问密钥。
    /// </summary>
    public const string RemoteSettingsRequired = "files.storage_config.remote_required";

    /// <summary>
    /// 服务地址不是 http / https 的绝对地址。
    /// </summary>
    public const string EndpointInvalid = "files.storage_config.endpoint_invalid";

    /// <summary>
    /// 文件超过大小上限。
    /// </summary>
    public const string FileTooLarge = "files.file.too_large";

    /// <summary>
    /// 文件为空或没有文件名。
    /// </summary>
    public const string FileEmpty = "files.file.empty";

    /// <summary>
    /// SHA-256 不是 64 位十六进制。
    /// </summary>
    public const string HashInvalid = "files.file.hash_invalid";

    /// <summary>
    /// 下载地址有效期超出范围。
    /// </summary>
    public const string LinkLifetimeInvalid = "files.link.lifetime_invalid";

    /// <summary>
    /// 文件状态只能设为正常或已归档。
    /// </summary>
    public const string FileStatusInvalid = "files.file.status_invalid";

    /// <summary>
    /// 副本状态只能设为正常或停用。
    /// </summary>
    public const string FileStorageStatusInvalid = "files.file_storage.status_invalid";

    /// <summary>
    /// 不支持的筛选。
    /// </summary>
    public const string FilterUnsupported = "files.query.filter_unsupported";

    /// <summary>
    /// 不支持的排序。
    /// </summary>
    public const string SortUnsupported = "files.query.sort_unsupported";

    /// <summary>
    /// 存储配置编码在本侧已被占用。
    /// </summary>
    public const string StorageCodeTaken = "files.storage_config.code_taken";

    /// <summary>
    /// 缺省存储配置不能停用或删除。
    /// </summary>
    public const string DefaultStorageLocked = "files.storage_config.default_locked";

    /// <summary>
    /// 存储配置还有文件副本，不能删除。
    /// </summary>
    public const string StorageConfigInUse = "files.storage_config.in_use";

    /// <summary>
    /// 副本不能作为主副本。
    /// </summary>
    public const string FileStorageNotUsable = "files.file_storage.not_usable";

    /// <summary>
    /// 主副本不能停用。
    /// </summary>
    public const string PrimaryStorageLocked = "files.file_storage.primary_locked";

    /// <summary>
    /// 文件已归档或已损坏，读不到内容。
    /// </summary>
    public const string FileUnavailable = "files.file.unavailable";

    /// <summary>
    /// 停用的存储配置不能设为缺省。
    /// </summary>
    public const string DefaultNeedsActive = "files.storage_config.default_needs_active";

    /// <summary>
    /// 这个文件要求持有 files.files.read。
    /// </summary>
    public const string FileAccessDenied = "files.file.access_denied";

    /// <summary>
    /// 没有可用的缺省存储配置。
    /// </summary>
    public const string NoStorageConfig = "files.storage.none";

    /// <summary>
    /// 存储配置的访问密钥解不开。
    /// </summary>
    public const string SecretUndecryptable = "files.storage_config.undecryptable";

    /// <summary>
    /// 操作 <c>export-tasks.submit</c> 的说明。
    /// </summary>
    public const string SubmitExportTaskDescription = "export_tasks.submit.description";

    /// <summary>
    /// 操作 <c>export-tasks.list</c> 的说明。
    /// </summary>
    public const string ListExportTasksDescription = "export_tasks.list.description";

    /// <summary>
    /// 操作 <c>export-tasks.get</c> 的说明。
    /// </summary>
    public const string GetExportTaskDescription = "export_tasks.get.description";

    /// <summary>
    /// 操作 <c>export-tasks.cancel</c> 的说明。
    /// </summary>
    public const string CancelExportTaskDescription = "export_tasks.cancel.description";

    /// <summary>
    /// 操作 <c>export-tasks.delete</c> 的说明。
    /// </summary>
    public const string DeleteExportTasksDescription = "export_tasks.delete.description";

    /// <summary>
    /// 操作 <c>import-histories.create</c> 的说明。
    /// </summary>
    public const string CreateImportHistoryDescription = "import_histories.create.description";

    /// <summary>
    /// 操作 <c>import-histories.recent</c> 的说明。
    /// </summary>
    public const string RecentImportHistoriesDescription = "import_histories.recent.description";

    /// <summary>
    /// 导出任务不存在。
    /// </summary>
    public const string ExportTaskNotFound = "files.export.not_found";

    /// <summary>
    /// 要导出的不是列表操作。
    /// </summary>
    public const string ExportTargetInvalid = "files.export.target_invalid";

    /// <summary>
    /// 导出至少要一列。
    /// </summary>
    public const string ExportColumnsRequired = "files.export.columns_required";

    /// <summary>
    /// 查询快照不是 JSON 对象。
    /// </summary>
    public const string ExportQueryInvalid = "files.export.query_invalid";

    /// <summary>
    /// 导出任务不在排队中。
    /// </summary>
    public const string ExportNotPending = "files.export.not_pending";

    /// <summary>
    /// 没有可带给后台执行的登录凭据。
    /// </summary>
    public const string ExportCredentialMissing = "files.export.credential_missing";

    /// <summary>
    /// 取消导出时写进任务的原因。
    /// </summary>
    public const string ExportCancelled = "files.export.cancelled";

    /// <summary>
    /// 导出结果文件的备注（{name}）。
    /// </summary>
    public const string ExportResultRemark = "files.export.result_remark";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(FilesMessages)));

    /// <summary>
    /// 消息目录（嵌入资源里的十种区域）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
