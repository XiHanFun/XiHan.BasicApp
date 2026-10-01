// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 生成到项目的写入器（位置由配置推导，不接受任意路径；默认关闭，fail-closed）
/// </summary>
public interface IGeneratedArtifactWriter
{
    /// <summary>
    /// 把产物写进项目：后端产物进与命名空间同名的后端项目，前端产物进前端工程
    /// </summary>
    /// <param name="artifacts">产物清单</param>
    /// <param name="backendProject">后端项目名（即表配置的命名空间，如 XiHan.BasicApp.Sample）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<GeneratedArtifactWriteResult> WriteToProjectAsync(IReadOnlyList<GeneratedArtifact> artifacts, string? backendProject, CancellationToken cancellationToken = default);
}

/// <summary>
/// 写入结果
/// </summary>
/// <param name="Success">是否成功</param>
/// <param name="Message">失败原因</param>
/// <param name="WrittenCount">写入文件数</param>
/// <param name="SkippedCount">跳过文件数（手动文件已存在）</param>
/// <param name="SkippedPaths">被跳过的相对路径清单</param>
/// <param name="TargetRoots">写入的项目目录（后端项目目录、前端工程目录）</param>
public sealed record GeneratedArtifactWriteResult(
    bool Success,
    string? Message,
    int WrittenCount,
    int SkippedCount,
    IReadOnlyList<string> SkippedPaths,
    IReadOnlyList<string> TargetRoots)
{
    /// <summary>成功结果</summary>
    public static GeneratedArtifactWriteResult Ok(int writtenCount, int skippedCount, IReadOnlyList<string> skippedPaths, IReadOnlyList<string> targetRoots)
        => new(true, null, writtenCount, skippedCount, skippedPaths, targetRoots);

    /// <summary>失败结果</summary>
    public static GeneratedArtifactWriteResult Fail(string message) => new(false, message, 0, 0, [], []);
}
