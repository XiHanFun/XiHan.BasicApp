// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.StaticFiles;
using XiHan.BasicApp.Files.Domain.Entities;

namespace XiHan.BasicApp.Files.Domain.Services;

/// <summary>
/// 判定文件大类与内容类型
/// </summary>
/// <remarks>
/// 4.x 同：先看内容类型，再看扩展名；上传方没给内容类型时按扩展名推，推不出为 <c>application/octet-stream</c>
/// </remarks>
public static class FileKinds
{
    private static readonly FileExtensionContentTypeProvider _contentTypes = new();

    private static readonly HashSet<string> _inlineTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp", "image/bmp", "image/avif", "application/pdf", "text/plain",
    };

    private static readonly HashSet<string> _documents = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".md", ".csv", ".rtf", ".odt", ".ods", ".odp", ".json", ".xml", ".html", ".htm",
    };

    private static readonly HashSet<string> _archives = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".bz2", ".xz",
    };

    /// <summary>
    /// 规范化的扩展名（小写含点，超过 20 个字符或没有为空）
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>扩展名</returns>
    public static string? ExtensionOf(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrEmpty(extension) || extension.Length > 20 || extension.Any(static c => !char.IsAsciiLetterOrDigit(c) && c != '.')
            ? null
            : extension.ToLowerInvariant();
    }

    /// <summary>
    /// 内容类型：上传方给了就用它，没有按扩展名推
    /// </summary>
    /// <param name="declared">上传方给的内容类型</param>
    /// <param name="fileName">文件名</param>
    /// <returns>内容类型</returns>
    public static string ContentTypeOf(string? declared, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(declared) && declared.Length <= 100 && !string.Equals(declared, "application/octet-stream", StringComparison.OrdinalIgnoreCase) && MediaTypeHeaderValue.TryParse(declared.Trim(), out _))
        {
            return declared.Trim();
        }

        return _contentTypes.TryGetContentType(fileName, out var guessed) ? guessed : "application/octet-stream";
    }

    /// <summary>
    /// 这个内容类型能不能在浏览器里内联打开
    /// </summary>
    /// <remarks>
    /// 只放行跑不了脚本的类型。内容类型是上传方给的，HTML、SVG 内联打开时脚本运行在应用的源上，能读走本地存的令牌，要以附件下载
    /// </remarks>
    /// <param name="contentType">内容类型</param>
    /// <returns>能内联时为 <see langword="true"/></returns>
    public static bool IsInlineSafe(string contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        if (!MediaTypeHeaderValue.TryParse(contentType, out var parsed) || parsed.MediaType is not { } mediaType)
        {
            return false;
        }

        return _inlineTypes.Contains(mediaType)
            || mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
            || mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 文件大类
    /// </summary>
    /// <param name="contentType">内容类型</param>
    /// <param name="extension">扩展名</param>
    /// <returns>大类</returns>
    public static FileKind KindOf(string contentType, string? extension)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return FileKind.Image;
        }

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return FileKind.Video;
        }

        if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return FileKind.Audio;
        }

        if (extension is not null && _archives.Contains(extension))
        {
            return FileKind.Archive;
        }

        if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) || (extension is not null && _documents.Contains(extension)))
        {
            return FileKind.Document;
        }

        return FileKind.Other;
    }
}
