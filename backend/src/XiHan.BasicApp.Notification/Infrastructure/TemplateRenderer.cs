// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;
using XiHan.BasicApp.Runtime.Templates;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 消息模板的渲染（Scriban）
/// </summary>
/// <remarks>
/// 方案 5.11。变量写成双花括号包住的变量名，4.x 的写法照样可用
/// 不给模板加载器（不能引入别的文件），循环与递归设上限，语法树限深（<see cref="TemplateLimits"/>），一次渲染最多 5 秒；HTML 模板里的参数值先做 HTML 编码，参数里的尖括号不会变成标签
/// </remarks>
internal static class TemplateRenderer
{
    private const int LoopLimit = 1000;
    private const int RecursiveLimit = 20;
    private const string TooDeep = "The template is nested too deeply.";
    private static readonly TimeSpan _renderTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// 检查模板语法
    /// </summary>
    /// <param name="source">模板源文</param>
    /// <returns>有错返回第一条错误的文字，没错返回空</returns>
    public static string? SyntaxError(string? source)
    {
        var template = Template.Parse(source ?? string.Empty);
        if (template.HasErrors)
        {
            return string.Join("; ", template.Messages.Select(static m => m.ToString()));
        }

        return TemplateLimits.IsTooDeep(template) ? TooDeep : null;
    }

    /// <summary>
    /// 渲染
    /// </summary>
    /// <param name="source">模板源文</param>
    /// <param name="parameters">参数</param>
    /// <param name="html">是否 HTML 模板（参数值先编码）</param>
    /// <param name="output">渲染结果</param>
    /// <param name="error">失败原因</param>
    /// <returns>是否成功</returns>
    public static bool TryRender(string? source, IReadOnlyList<KeyValuePair<string, string?>> parameters, bool html, out string output, out string? error)
    {
        output = string.Empty;
        var template = Template.Parse(source ?? string.Empty);
        if (template.HasErrors)
        {
            error = string.Join("; ", template.Messages.Select(static m => m.ToString()));
            return false;
        }

        if (TemplateLimits.IsTooDeep(template))
        {
            error = TooDeep;
            return false;
        }

        var model = new ScriptObject();
        foreach (var (name, value) in parameters)
        {
            model.SetValue(name, html && value is not null ? WebUtility.HtmlEncode(value) : value, readOnly: false);
        }

        using var timeout = new CancellationTokenSource(_renderTimeout);
        var context = new TemplateContext { LoopLimit = LoopLimit, RecursiveLimit = RecursiveLimit, StrictVariables = false, RegexTimeOut = _regexTimeout, CancellationToken = timeout.Token };
        context.PushGlobal(model);
        try
        {
            output = template.Render(context);
            error = null;
            return true;
        }
        catch (ScriptRuntimeException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            error = "The template took too long to render.";
            return false;
        }
    }
}
