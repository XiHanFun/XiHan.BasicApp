// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 模板渲染器解析器：按 <see cref="TemplateEngine"/> 在已注册的渲染器中选择
/// </summary>
public sealed class TemplateRendererResolver : ITemplateRendererResolver
{
    private readonly IReadOnlyDictionary<TemplateEngine, ITemplateRenderer> _renderers;

    /// <summary>
    /// 构造函数（注入全部已注册的渲染器）
    /// </summary>
    public TemplateRendererResolver(IEnumerable<ITemplateRenderer> renderers)
    {
        ArgumentNullException.ThrowIfNull(renderers);

        // 后注册覆盖先注册（便于自定义渲染器替换默认实现）
        var map = new Dictionary<TemplateEngine, ITemplateRenderer>();
        foreach (var renderer in renderers)
        {
            map[renderer.Engine] = renderer;
        }

        _renderers = map;
    }

    /// <inheritdoc />
    public ITemplateRenderer Resolve(TemplateEngine engine)
    {
        return _renderers.TryGetValue(engine, out var renderer)
            ? renderer
            : throw new NotSupportedException($"未注册 {engine} 模板渲染器。当前仅支持 Scriban。");
    }
}
