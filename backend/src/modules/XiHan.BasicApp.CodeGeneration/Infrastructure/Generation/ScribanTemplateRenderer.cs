#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ScribanTemplateRenderer
// Guid:c0de9e00-0301-4a00-9000-000000000301
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.Framework.Templating.Services;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// Scriban 模板渲染器（接通框架 ITemplateService）
/// </summary>
/// <remarks>
/// 这是本轮唯一落地的渲染通道。运行时依赖 Templating 模块注册的 <see cref="ITemplateService"/>。
/// </remarks>
public sealed class ScribanTemplateRenderer(ITemplateService templateService) : ITemplateRenderer
{
    private readonly ITemplateService _templateService = templateService;

    /// <inheritdoc />
    public TemplateEngine Engine => TemplateEngine.Scriban;

    /// <inheritdoc />
    public async Task<string> RenderAsync(string templateSource, CodeGenerationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(templateSource))
        {
            return string.Empty;
        }

        // 直接把强类型上下文作为模型交给 Scriban 渲染
        return await _templateService.RenderAsync(templateSource, context);
    }

    /// <inheritdoc />
    public TemplateRenderValidation Validate(string templateSource)
    {
        if (string.IsNullOrWhiteSpace(templateSource))
        {
            return TemplateRenderValidation.Invalid("模板内容为空");
        }

        var validation = _templateService.ValidateTemplate(templateSource);
        return validation.IsValid
            ? TemplateRenderValidation.Valid()
            : TemplateRenderValidation.Invalid(validation.ErrorMessage ?? "模板语法错误");
    }
}
