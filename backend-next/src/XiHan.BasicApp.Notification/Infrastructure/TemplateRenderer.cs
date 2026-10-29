// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 消息模板的渲染（Scriban，方案 5.11）：变量写成双花括号包住的变量名，4.x 的写法照样可用。
/// 不给模板加载器（不能引入别的文件），循环与递归设上限；HTML 模板里的参数值先做 HTML 编码，参数里的尖括号不会变成标签。
/// </summary>
internal static class TemplateRenderer
{
    private const int LoopLimit = 1000;
    private const int RecursiveLimit = 20;

    /// <summary>
    /// 检查语法；有错返回第一条错误的文字，没错返回空。
    /// </summary>
    /// <param name="source">模板源文。</param>
    /// <returns>错误文字。</returns>
    public static string? SyntaxError(string? source)
    {
        var template = Template.Parse(source ?? string.Empty);
        return template.HasErrors ? string.Join("; ", template.Messages.Select(static m => m.ToString())) : null;
    }

    /// <summary>
    /// 渲染。
    /// </summary>
    /// <param name="source">模板源文。</param>
    /// <param name="parameters">参数。</param>
    /// <param name="html">是否 HTML 模板（参数值先编码）。</param>
    /// <param name="output">渲染结果。</param>
    /// <param name="error">失败原因。</param>
    /// <returns>是否成功。</returns>
    public static bool TryRender(string? source, IReadOnlyList<KeyValuePair<string, string?>> parameters, bool html, out string output, out string? error)
    {
        output = string.Empty;
        var template = Template.Parse(source ?? string.Empty);
        if (template.HasErrors)
        {
            error = string.Join("; ", template.Messages.Select(static m => m.ToString()));
            return false;
        }

        var model = new ScriptObject();
        foreach (var (name, value) in parameters)
        {
            model.SetValue(name, html && value is not null ? WebUtility.HtmlEncode(value) : value, readOnly: false);
        }

        var context = new TemplateContext { LoopLimit = LoopLimit, RecursiveLimit = RecursiveLimit, StrictVariables = false };
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
    }
}
