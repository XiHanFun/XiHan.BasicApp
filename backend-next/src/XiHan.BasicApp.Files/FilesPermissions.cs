// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件包的权限码。
/// </summary>
/// <remarks>
/// 两侧都有，各自管理本侧的文件与存储配置。
/// 换取访问级别为「已登录」的文件的下载地址只要求已登录，不在这里。
/// </remarks>
public static class FilesPermissions
{
    /// <summary>
    /// 查看文件与副本，换取任意文件的下载地址。
    /// </summary>
    public const string FilesRead = "files.files.read";

    /// <summary>
    /// 上传文件（含秒传）。
    /// </summary>
    public const string FilesCreate = "files.files.create";

    /// <summary>
    /// 修改文件信息、归档与恢复、管理副本。
    /// </summary>
    public const string FilesUpdate = "files.files.update";

    /// <summary>
    /// 删除文件。
    /// </summary>
    public const string FilesDelete = "files.files.delete";

    /// <summary>
    /// 查看存储配置。
    /// </summary>
    public const string StorageConfigsRead = "files.storage-configs.read";

    /// <summary>
    /// 新建存储配置。
    /// </summary>
    public const string StorageConfigsCreate = "files.storage-configs.create";

    /// <summary>
    /// 修改、启停存储配置与设为缺省。
    /// </summary>
    public const string StorageConfigsUpdate = "files.storage-configs.update";

    /// <summary>
    /// 删除存储配置。
    /// </summary>
    public const string StorageConfigsDelete = "files.storage-configs.delete";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(FilesRead, "Read files", PermissionScope.Both, description: "View the caller's side's files and their copies, and obtain download links for any of them."),
        new(FilesCreate, "Upload files", PermissionScope.Both, audited: true, description: "Upload files to the caller's side, including instant upload of content that is already stored."),
        new(FilesUpdate, "Update files", PermissionScope.Both, audited: true, description: "Edit file details, archive and restore files, and manage their stored copies."),
        new(FilesDelete, "Delete files", PermissionScope.Both, audited: true, description: "Delete files on the caller's side; stored content goes once no other file uses it."),
        new(StorageConfigsRead, "Read storage configurations", PermissionScope.Both, description: "View the caller's side's storage configurations; access keys are never shown."),
        new(StorageConfigsCreate, "Create storage configurations", PermissionScope.Both, audited: true, description: "Create local or S3-compatible storage configurations on the caller's side."),
        new(StorageConfigsUpdate, "Update storage configurations", PermissionScope.Both, audited: true, description: "Edit, enable, disable and set the default storage configuration on the caller's side."),
        new(StorageConfigsDelete, "Delete storage configurations", PermissionScope.Both, audited: true, description: "Delete storage configurations on the caller's side that are not the default and hold no file copies."),
    ];
}
