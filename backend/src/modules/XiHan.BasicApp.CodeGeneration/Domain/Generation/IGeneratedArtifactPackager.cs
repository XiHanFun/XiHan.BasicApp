// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 生成产物打包器：把多文件产物打包为可下载字节流（Zip）
/// </summary>
public interface IGeneratedArtifactPackager
{
    /// <summary>
    /// 打包产物
    /// </summary>
    /// <param name="artifacts">产物清单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>压缩包字节流</returns>
    Task<byte[]> PackAsync(IEnumerable<GeneratedArtifact> artifacts, CancellationToken cancellationToken = default);
}
