// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 代码生成引擎：管线统一编排入口（建模 → 选模板 → 渲染 → 产出）
/// </summary>
/// <remarks>
/// 应用层编排服务只负责权限/事务/DTO 转换/历史留痕，生成步骤全部下沉到本引擎。
/// </remarks>
public interface ICodeGenerationEngine
{
    /// <summary>
    /// 预览生成（仅返回产物内容，不打包、不落盘）
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成结果（含产物清单）</returns>
    Task<GenerationResult> PreviewAsync(GenerationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行生成（按 GenType 分流：Zip 打包 / 落盘 / 预览）
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成结果（Zip 时含 Package 字节流）</returns>
    Task<GenerationResult> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken = default);
}
