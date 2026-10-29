// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件包的页面登记：文件库、存储配置、导出中心。
/// </summary>
/// <remarks>
/// 两侧都有。
/// </remarks>
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
    /// 导出中心页。
    /// </summary>
    /// <remarks>
    /// 只看自己的导出，登录即可；4.x 同不设权限。
    /// </remarks>
    public static PageDescriptor ExportCenter { get; } = new("files.export-center", "/files/export-center", "files.export_center.title", "lucide:file-down", 470);

    /// <summary>
    /// 全部顶层页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Library, StorageConfigs, ExportCenter];
}
