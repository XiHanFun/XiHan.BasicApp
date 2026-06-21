#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IGeneratedArtifactPackager
// Guid:c0de9e00-0006-4a00-9000-000000000006
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
