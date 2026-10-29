// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 检查每个操作的暴露面与操作表一致。
/// </summary>
/// <remarks>
/// 方案 5.4 / 第 10 节第 8 项的操作表：管理操作只 <c>Http</c>，选中的只读目录类操作加 <c>Mcp | Ai</c>，供内部服务调用的加 <c>Grpc</c>。
/// 超出 <c>Http</c> 的操作逐个登记在这里，没登记的必须恰好是 <c>Http</c>，暴露面的每次变化都要改这张表。
/// </remarks>
public sealed class OperationExposureTests
{
    /// <summary>
    /// 只读目录类操作的暴露面：HTTP、MCP 工具与 AI 工具目录。
    /// </summary>
    private const Expose DirectoryRead = Expose.Http | Expose.Mcp | Expose.Ai;

    /// <summary>
    /// 超出 <c>Http</c> 的操作及其暴露面。
    /// </summary>
    /// <remarks>
    /// 这些操作都必须只读：动词是 <c>get</c> / <c>list</c>，或声明了 <c>ReadOnly = true</c>（ADR-0041，
    /// 框架据此给 MCP 只读提示，并让没有嵌套对象的请求走 HTTP GET）。
    /// </remarks>
    private static readonly Dictionary<string, Expose> _beyondHttp = new(StringComparer.Ordinal)
    {
        // 用户目录，同时是内部服务查用户的 gRPC 面；gRPC 与 MCP 的冒烟落在这两个上
        ["users.get"] = DirectoryRead | Expose.Grpc,
        ["users.list"] = DirectoryRead | Expose.Grpc,
        ["departments.get"] = DirectoryRead,
        ["departments.tree"] = DirectoryRead,
        ["positions.get"] = DirectoryRead,
        ["positions.list"] = DirectoryRead,
        ["memberships.list"] = DirectoryRead,
        ["permissions.list"] = DirectoryRead,
        // 字典是 5.4 点名的目录类：字典、字典项与业务表单要的选项
        ["dicts.list"] = DirectoryRead,
        ["dicts.get"] = DirectoryRead,
        ["dicts.options"] = DirectoryRead,
        ["dict-items.list"] = DirectoryRead,
    };

    /// <summary>
    /// 框架按动词推导为只读且幂等的两个动词。
    /// </summary>
    /// <remarks>
    /// 其他动词要显式声明 <c>ReadOnly</c>。
    /// </remarks>
    private static readonly HashSet<string> _readOnlyVerbs = new(StringComparer.Ordinal) { "get", "list" };

    [Fact]
    public void EveryOperation_MatchesTheOperationTable()
    {
        var mismatches = OperationContractTests.Operations()
            .Select(static o => (o.Attribute.Id, o.Attribute.Expose, Expected: _beyondHttp.GetValueOrDefault(o.Attribute.Id, Expose.Http)))
            .Where(static o => o.Expose != o.Expected)
            .Select(static o => $"{o.Id}: Expose = {o.Expose}, the operation table says {o.Expected}")
            .ToList();

        Assert.True(mismatches.Count == 0, "Operations whose Expose differs from the operation table:" + Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }

    [Fact]
    public void OperationTable_ListsOnlyExistingReadOnlyOperations()
    {
        var operations = OperationContractTests.Operations().ToDictionary(static o => o.Attribute.Id, static o => o.Attribute, StringComparer.Ordinal);
        foreach (var (id, expose) in _beyondHttp)
        {
            Assert.True(operations.TryGetValue(id, out var attribute), $"The operation table lists '{id}', which is not an operation");
            Assert.True(_readOnlyVerbs.Contains(OperationId.VerbOf(id)) || attribute.ReadOnly, $"'{id}' is exposed beyond HTTP but is not read-only (verb get / list, or ReadOnly = true)");
            Assert.True(expose.HasFlag(Expose.Http), $"'{id}' must keep HTTP");
        }
    }
}
