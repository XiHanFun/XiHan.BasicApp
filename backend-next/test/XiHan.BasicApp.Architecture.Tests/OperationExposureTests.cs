// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 方案 5.4 / 第 10 节第 8 项的操作表：管理操作只 <c>Http</c>，选中的只读目录类操作加 <c>Mcp | Ai</c>，供内部服务调用的加 <c>Grpc</c>。
/// 超出 <c>Http</c> 的操作逐个登记在这里，没登记的必须恰好是 <c>Http</c>，暴露面的每次变化都要改这张表。
/// </summary>
public sealed class OperationExposureTests
{
    /// <summary>
    /// 只读目录类操作的暴露面：HTTP、MCP 工具与 AI 工具目录。
    /// </summary>
    private const Expose DirectoryRead = Expose.Http | Expose.Mcp | Expose.Ai;

    /// <summary>
    /// 超出 <c>Http</c> 的操作。只读按动词判：框架给 MCP 的行为提示只认 <c>get</c> / <c>list</c> 为只读，
    /// 其他只读动词（<c>departments.tree</c>）要等框架能声明只读（方案 9.4）后再登记。
    /// </summary>
    private static readonly Dictionary<string, Expose> _beyondHttp = new(StringComparer.Ordinal)
    {
        // 用户目录，同时是内部服务查用户的 gRPC 面；gRPC 与 MCP 的冒烟落在这两个上
        ["users.get"] = DirectoryRead | Expose.Grpc,
        ["users.list"] = DirectoryRead | Expose.Grpc,
        ["departments.get"] = DirectoryRead,
        ["positions.get"] = DirectoryRead,
        ["positions.list"] = DirectoryRead,
        ["memberships.list"] = DirectoryRead,
        ["permissions.list"] = DirectoryRead,
    };

    /// <summary>
    /// 框架把这两个动词推导为只读且幂等（MCP 的 <c>readOnlyHint</c>）。
    /// </summary>
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
        var ids = OperationContractTests.Operations().Select(static o => o.Attribute.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var (id, expose) in _beyondHttp)
        {
            Assert.True(ids.Contains(id), $"The operation table lists '{id}', which is not an operation");
            Assert.True(_readOnlyVerbs.Contains(OperationId.VerbOf(id)), $"'{id}' is exposed beyond HTTP but its verb is not read-only (get / list)");
            Assert.True(expose.HasFlag(Expose.Http), $"'{id}' must keep HTTP");
        }
    }
}
