// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 单引擎模板渲染器（每种 <see cref="TemplateEngine"/> 一个实现）
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// 渲染器对应的模板引擎
    /// </summary>
    TemplateEngine Engine { get; }

    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="templateSource">模板源码</param>
    /// <param name="context">代码生成上下文（模板模型）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>渲染结果文本</returns>
    Task<string> RenderAsync(string templateSource, CodeGenerationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验模板语法
    /// </summary>
    /// <param name="templateSource">模板源码</param>
    /// <returns>校验结果</returns>
    TemplateRenderValidation Validate(string templateSource);
}
