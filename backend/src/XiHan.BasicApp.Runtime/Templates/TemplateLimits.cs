// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Scriban;
using Scriban.Syntax;

namespace XiHan.BasicApp.Runtime.Templates;

/// <summary>
/// 模板语法树的深度上限
/// </summary>
/// <remarks>
/// Scriban 的循环与递归上限管不到成员链和嵌套表达式：<c>{{ a.a.a… }}</c> 能解析通过，求值时按语法树递归，一千多层就把线程栈撑爆，整个进程退出，接不住
/// 模板是用户写的（消息模板租户管理员就能写），所以保存与渲染之前都要判，遍历不递归
/// </remarks>
public static class TemplateLimits
{
    /// <summary>
    /// 语法树最多多少层
    /// </summary>
    /// <remarks>
    /// 一层语句嵌套占三四层，正常模板到不了；这个深度在 1.5 MB 的线程栈上渲染是安全的
    /// </remarks>
    public const int MaxDepth = 256;

    /// <summary>
    /// 语法树是否超过上限
    /// </summary>
    /// <param name="template">解析好的模板</param>
    /// <returns>超过时为 <see langword="true"/></returns>
    public static bool IsTooDeep(Template template)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (template.Page is null)
        {
            return false;
        }

        var pending = new Stack<(ScriptNode Node, int Depth)>();
        pending.Push((template.Page, 1));
        while (pending.TryPop(out var current))
        {
            if (current.Depth > MaxDepth)
            {
                return true;
            }

            foreach (var child in current.Node.Children)
            {
                if (child is not null)
                {
                    pending.Push((child, current.Depth + 1));
                }
            }
        }

        return false;
    }
}
