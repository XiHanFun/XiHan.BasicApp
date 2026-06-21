#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITemplateRenderer
// Guid:c0de9e00-0002-4a00-9000-000000000002
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
