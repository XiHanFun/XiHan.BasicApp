#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IGeneratedArtifactWriter
// Guid:c0de9e00-0009-4a00-9000-000000000009
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 生成产物落盘写入器（受控目录写入，白名单 + 路径穿越校验 fail-closed）
/// </summary>
public interface IGeneratedArtifactWriter
{
    /// <summary>
    /// 将产物写入目标根目录（须命中白名单且不越界，否则整体拒绝）
    /// </summary>
    Task<GeneratedArtifactWriteResult> WriteAsync(IReadOnlyList<GeneratedArtifact> artifacts, string? targetRootPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// 落盘结果
/// </summary>
/// <param name="Success">是否成功</param>
/// <param name="Message">失败原因</param>
/// <param name="WrittenCount">写入文件数</param>
public sealed record GeneratedArtifactWriteResult(bool Success, string? Message, int WrittenCount)
{
    /// <summary>成功结果</summary>
    public static GeneratedArtifactWriteResult Ok(int count) => new(true, null, count);

    /// <summary>失败结果</summary>
    public static GeneratedArtifactWriteResult Fail(string message) => new(false, message, 0);
}
