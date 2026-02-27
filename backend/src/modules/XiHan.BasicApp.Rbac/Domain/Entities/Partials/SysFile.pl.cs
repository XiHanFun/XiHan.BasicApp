#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFile.pl
// Guid:b3420698-492f-4911-ae7c-efbc29512e7d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Domain.Entities;

/// <summary>
/// 系统文件实体（部分类 - 业务方法）
/// </summary>
public partial class SysFile
{
    /// <summary>
    /// 文件存储列表（一对多）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysFileStorage.FileId))]
    public virtual List<SysFileStorage>? Storages { get; set; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    public virtual bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.Now;

    /// <summary>
    /// 获取主存储
    /// </summary>
    /// <returns></returns>
    public SysFileStorage? GetPrimaryStorage()
    {
        return Storages?.FirstOrDefault(s => s.IsPrimary && s.Status == FileStorageStatus.Normal);
    }

    /// <summary>
    /// 获取最优访问URL
    /// </summary>
    /// <param name="preferCdn">是否优先使用CDN</param>
    /// <returns></returns>
    public string? GetAccessUrl(bool preferCdn = true)
    {
        var storage = GetPrimaryStorage();
        if (storage == null)
        {
            return null;
        }

        if (preferCdn && storage.EnableCdn && !string.IsNullOrEmpty(storage.CdnUrl))
        {
            return storage.CdnUrl;
        }

        return storage.ExternalUrl ?? storage.InternalUrl;
    }

    /// <summary>
    /// 获取所有可用的存储位置
    /// </summary>
    /// <returns></returns>
    public List<SysFileStorage> GetAvailableStorages()
    {
        return Storages?.Where(s => s.Status == FileStorageStatus.Normal).OrderBy(s => s.SortOrder).ToList() ?? [];
    }

    /// <summary>
    /// 添加存储位置
    /// </summary>
    /// <param name="storage"></param>
    public void AddStorage(SysFileStorage storage)
    {
        Storages ??= [];

        // 如果是主存储，将其他存储的主存储标识设置为false
        if (storage.IsPrimary)
        {
            foreach (var existingStorage in Storages)
            {
                existingStorage.IsPrimary = false;
            }
        }

        storage.FileId = BasicId;
        Storages.Add(storage);
    }

    /// <summary>
    /// 设置主存储
    /// </summary>
    /// <param name="storageId"></param>
    public void SetPrimaryStorage(long storageId)
    {
        if (Storages == null)
        {
            return;
        }

        foreach (var storage in Storages)
        {
            storage.IsPrimary = storage.BasicId == storageId;
        }
    }

    /// <summary>
    /// 增加下载次数
    /// </summary>
    public void IncrementDownloadCount()
    {
        DownloadCount++;
        LastDownloadTime = DateTimeOffset.Now;
    }

    /// <summary>
    /// 增加访问次数
    /// </summary>
    public void IncrementViewCount()
    {
        ViewCount++;
        LastAccessTime = DateTimeOffset.Now;
    }

    /// <summary>
    /// 检查文件是否可访问
    /// </summary>
    /// <returns></returns>
    public bool IsAccessible()
    {
        return Status == FileStatus.Normal && !IsExpired;
    }

    /// <summary>
    /// 标记为已删除
    /// </summary>
    public void MarkAsDeleted()
    {
        Status = FileStatus.Deleted;
    }

    /// <summary>
    /// 标记为已归档
    /// </summary>
    public void MarkAsArchived()
    {
        Status = FileStatus.Archived;
    }

    /// <summary>
    /// 获取格式化的文件大小
    /// </summary>
    /// <returns></returns>
    public string GetFormattedSize()
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = FileSize;
        var order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 检查是否为图片
    /// </summary>
    /// <returns></returns>
    public bool IsImage()
    {
        return FileType == FileType.Image;
    }

    /// <summary>
    /// 检查是否为视频
    /// </summary>
    /// <returns></returns>
    public bool IsVideo()
    {
        return FileType == FileType.Video;
    }

    /// <summary>
    /// 检查是否为文档
    /// </summary>
    /// <returns></returns>
    public bool IsDocument()
    {
        return FileType == FileType.Document;
    }

    /// <summary>
    /// 验证文件哈希
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public bool VerifyHash(string hash)
    {
        return !string.IsNullOrEmpty(FileHash) && FileHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 设置过期时间（临时文件）
    /// </summary>
    /// <param name="hours">小时数</param>
    public void SetExpiration(int hours)
    {
        IsTemporary = true;
        ExpiresAt = DateTimeOffset.Now.AddHours(hours);
    }

    /// <summary>
    /// 添加标签
    /// </summary>
    /// <param name="tag"></param>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var tags = GetTagList();
        if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(tag);
            Tags = string.Join(",", tags);
        }
    }

    /// <summary>
    /// 移除标签
    /// </summary>
    /// <param name="tag"></param>
    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var tags = GetTagList();
        tags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
        Tags = tags.Count > 0 ? string.Join(",", tags) : null;
    }

    /// <summary>
    /// 获取标签列表
    /// </summary>
    /// <returns></returns>
    public List<string> GetTagList()
    {
        if (string.IsNullOrWhiteSpace(Tags))
        {
            return [];
        }

        return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    /// <summary>
    /// 检查是否有指定标签
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var tags = GetTagList();
        return tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
    }
}
