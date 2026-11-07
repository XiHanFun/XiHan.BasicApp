// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 菜单树断言的辅助：按编码在整棵树里找节点、把树写成一行轮廓
/// </summary>
/// <remarks>
/// <c>auth.permissions</c> 的 <c>menus</c> 与 <c>menus.list</c> 的 <c>items</c> 都是带 <c>children</c> 的树；页面缺省挂在内置目录下，顶层只剩工作台、几个中心目录与帮助中心
/// </remarks>
internal static class MenuTrees
{
    /// <summary>
    /// 同级节点的编码
    /// </summary>
    public static List<string?> Codes(JsonElement nodes) => nodes.EnumerateArray().Select(static m => m.GetProperty("code").GetString()).ToList();

    /// <summary>
    /// 整棵树里找编码对应的节点（深度优先）
    /// </summary>
    /// <exception cref="InvalidOperationException">树里没有这个编码或有多个时抛出</exception>
    public static JsonElement Find(JsonElement nodes, string code)
    {
        var matches = new List<JsonElement>();
        Walk(nodes);
        return matches.Single();

        void Walk(JsonElement items)
        {
            foreach (var item in items.EnumerateArray())
            {
                if (item.GetProperty("code").GetString() == code)
                {
                    matches.Add(item);
                }

                if (item.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
                {
                    Walk(children);
                }
            }
        }
    }

    /// <summary>
    /// 树的一行轮廓：同级以逗号分隔，有子节点的写成 <c>编码[子轮廓]</c>
    /// </summary>
    public static string Outline(JsonElement nodes)
        => string.Join(", ", nodes.EnumerateArray().Select(static item =>
            item.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array && children.GetArrayLength() > 0
                ? $"{item.GetProperty("code").GetString()}[{Outline(children)}]"
                : item.GetProperty("code").GetString()));
}
