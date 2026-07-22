// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 模板渲染器解析器：按 <see cref="TemplateEngine"/> 选择对应的 <see cref="ITemplateRenderer"/>
/// </summary>
public interface ITemplateRendererResolver
{
    /// <summary>
    /// 解析渲染器
    /// </summary>
    /// <param name="engine">模板引擎</param>
    /// <returns>渲染器实现</returns>
    /// <exception cref="NotSupportedException">引擎未实现渲染器时抛出</exception>
    ITemplateRenderer Resolve(TemplateEngine engine);
}
