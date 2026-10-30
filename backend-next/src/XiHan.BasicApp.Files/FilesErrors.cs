// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件包的错误码。
/// </summary>
/// <remarks>
/// 格式为 <c>FILES_&lt;KIND&gt;_&lt;NNN&gt;</c>。对象存储本身的失败沿用框架的 <c>OBJECTSTORAGE_*</c>。
/// </remarks>
public static class FilesErrors
{
    /// <summary>
    /// 文件不存在（或不在当前一侧）。
    /// </summary>
    public const string FileNotFound = "FILES_NOTFOUND_001";

    /// <summary>
    /// 存储配置不存在（或不在当前一侧）。
    /// </summary>
    public const string StorageConfigNotFound = "FILES_NOTFOUND_002";

    /// <summary>
    /// 副本不存在。
    /// </summary>
    public const string FileStorageNotFound = "FILES_NOTFOUND_003";

    /// <summary>
    /// 存储配置编码为空或含空白。
    /// </summary>
    public const string StorageCodeInvalid = "FILES_VALIDATION_001";

    /// <summary>
    /// 桶名不合规则。
    /// </summary>
    public const string BucketInvalid = "FILES_VALIDATION_002";

    /// <summary>
    /// S3 兼容存储缺服务地址、访问密钥 ID 或访问密钥。
    /// </summary>
    public const string RemoteSettingsRequired = "FILES_VALIDATION_003";

    /// <summary>
    /// 服务地址不是 http / https 的绝对地址。
    /// </summary>
    public const string EndpointInvalid = "FILES_VALIDATION_004";

    /// <summary>
    /// 文件超过大小上限。
    /// </summary>
    public const string FileTooLarge = "FILES_VALIDATION_005";

    /// <summary>
    /// 文件为空或没有文件名。
    /// </summary>
    public const string FileEmpty = "FILES_VALIDATION_006";

    /// <summary>
    /// SHA-256 不是 64 位十六进制。
    /// </summary>
    public const string HashInvalid = "FILES_VALIDATION_007";

    /// <summary>
    /// 下载地址有效期超出范围。
    /// </summary>
    public const string LinkLifetimeInvalid = "FILES_VALIDATION_008";

    /// <summary>
    /// 文件状态只能设为正常或已归档。
    /// </summary>
    public const string FileStatusInvalid = "FILES_VALIDATION_009";

    /// <summary>
    /// 副本状态只能设为正常或停用。
    /// </summary>
    public const string FileStorageStatusInvalid = "FILES_VALIDATION_010";

    /// <summary>
    /// 列表的筛选或排序字段不受支持。
    /// </summary>
    public const string QueryUnsupported = "FILES_VALIDATION_011";

    /// <summary>
    /// 存储配置编码在本侧已被占用。
    /// </summary>
    public const string StorageCodeTaken = "FILES_CONFLICT_001";

    /// <summary>
    /// 缺省存储配置不能停用或删除。
    /// </summary>
    public const string DefaultStorageLocked = "FILES_CONFLICT_002";

    /// <summary>
    /// 存储配置还有文件副本，不能删除。
    /// </summary>
    public const string StorageConfigInUse = "FILES_CONFLICT_003";

    /// <summary>
    /// 副本不能作为主副本（不是正常状态或没有校验过），或主副本不能停用。
    /// </summary>
    public const string FileStorageNotUsable = "FILES_CONFLICT_004";

    /// <summary>
    /// 文件已归档或已损坏，读不到内容。
    /// </summary>
    public const string FileUnavailable = "FILES_CONFLICT_005";

    /// <summary>
    /// 停用的存储配置不能设为缺省。
    /// </summary>
    public const string DefaultNeedsActive = "FILES_CONFLICT_006";

    /// <summary>
    /// 访问级别要求 <c>files.files.read</c>。
    /// </summary>
    public const string FileAccessDenied = "FILES_DENIED_001";

    /// <summary>
    /// 当前一侧与平台都没有可用的缺省存储配置。
    /// </summary>
    public const string NoStorageConfig = "FILES_UNAVAILABLE_001";

    /// <summary>
    /// 存储配置的访问密钥解不开（数据保护的密钥环换过）。
    /// </summary>
    public const string SecretUndecryptable = "FILES_UNAVAILABLE_002";

    /// <summary>
    /// 导出任务不存在（或不是自己的）。
    /// </summary>
    public const string ExportTaskNotFound = "FILES_NOTFOUND_005";

    /// <summary>
    /// 要导出的不是已登记的列表操作。
    /// </summary>
    public const string ExportTargetInvalid = "FILES_VALIDATION_012";

    /// <summary>
    /// 导出至少要一列。
    /// </summary>
    public const string ExportColumnsRequired = "FILES_VALIDATION_013";

    /// <summary>
    /// 查询快照不是 JSON 对象。
    /// </summary>
    public const string ExportQueryInvalid = "FILES_VALIDATION_014";

    /// <summary>
    /// 导出任务不在排队中（执行中的不能取消或删除）。
    /// </summary>
    public const string ExportNotPending = "FILES_CONFLICT_007";

    /// <summary>
    /// 这次调用没有可以带给后台执行的登录凭据。
    /// </summary>
    public const string ExportCredentialMissing = "FILES_UNAVAILABLE_003";

    /// <summary>
    /// 分片上传不存在（或不是自己发起的）。
    /// </summary>
    public const string UploadNotFound = "FILES_NOTFOUND_006";

    /// <summary>
    /// 分片编号不在这次上传的范围里。
    /// </summary>
    public const string PartNumbersInvalid = "FILES_VALIDATION_015";

    /// <summary>
    /// 分片上传已结束（完成、放弃、过期或失败）。
    /// </summary>
    public const string UploadNotActive = "FILES_CONFLICT_008";

    /// <summary>
    /// 还有分片没有上传。
    /// </summary>
    public const string UploadPartsIncomplete = "FILES_CONFLICT_009";

    /// <summary>
    /// 已上传的分片与声明的大小不符。
    /// </summary>
    public const string UploadSizeMismatch = "FILES_CONFLICT_010";

    /// <summary>
    /// 上传目标的存储不支持分片上传。
    /// </summary>
    public const string MultipartUnsupported = "FILES_UNAVAILABLE_004";
}
