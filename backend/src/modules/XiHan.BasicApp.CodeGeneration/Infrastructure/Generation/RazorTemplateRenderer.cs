#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RazorTemplateRenderer
// Guid:c0de9e00-0302-4a00-9000-000000000302
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// Razor 模板渲染器（占位，未实现）
/// </summary>
/// <remarks>
/// 决策 D1：本轮只实现 Scriban 通道。Razor 需额外引入 RazorLight 等运行时编译能力，框架当前不支持。
/// 保留此实现以维持多引擎抽象的完整性；选用 Razor 引擎时显式抛出 <see cref="NotSupportedException"/>。
/// </remarks>
public sealed class RazorTemplateRenderer : ITemplateRenderer
{
    private const string NotSupportedMessage = "Razor 渲染器尚未实现（仅支持 Scriban）。请将模板引擎设置为 Scriban，或在后续阶段接入 RazorLight。";

    /// <inheritdoc />
    public TemplateEngine Engine => TemplateEngine.Razor;

    /// <inheritdoc />
    public Task<string> RenderAsync(string templateSource, CodeGenerationContext context, CancellationToken cancellationToken = default)
        // TODO(S2): 接入 RazorLight 运行时编译渲染
        => throw new NotSupportedException(NotSupportedMessage);

    /// <inheritdoc />
    public TemplateRenderValidation Validate(string templateSource)
        => TemplateRenderValidation.Invalid(NotSupportedMessage);
}
