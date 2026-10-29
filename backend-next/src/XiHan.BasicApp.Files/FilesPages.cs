// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件包的页面登记：文件库、存储配置。两侧都有。
/// </summary>
public static class FilesPages
{
    /// <summary>
    /// 文件库页。
    /// </summary>
    public static PageDescriptor Library { get; } = new("files.library", "/files/library", "files.library.title", "lucide:folder-open", 450, FilesPermissions.FilesRead);

    /// <summary>
    /// 存储配置页。
    /// </summary>
    public static PageDescriptor StorageConfigs { get; } = new("files.storage-configs", "/files/storage-configs", "files.storage_configs.title", "lucide:hard-drive", 460, FilesPermissions.StorageConfigsRead);

    /// <summary>
    /// 全部顶层页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Library, StorageConfigs];
}
