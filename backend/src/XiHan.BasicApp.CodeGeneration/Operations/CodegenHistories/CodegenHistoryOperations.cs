// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenHistories;

/// <summary>
/// 分页列出生成历史
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-histories:list</c>。筛选与排序见 <see cref="CodegenHistoryQueries"/>
/// </remarks>
/// <param name="queries">生成历史的查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.HistoriesRead)]
public sealed class ListCodegenHistories(CodegenHistoryQueries queries) : IOperation<PageRequest, PageResult<CodegenHistoryResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-histories.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<CodegenHistoryResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 生成历史详情
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-histories/{id}</c>。带产物文件、用到的模板、表配置快照与失败原因（照 4.x）；产物包从 <c>GET /api/codegen-histories/{id}/package</c> 下载
/// </remarks>
/// <param name="queries">生成历史的查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.HistoriesRead)]
public sealed class GetCodegenHistory(CodegenHistoryQueries queries) : IOperation<CodegenHistoryIdRequest, CodegenHistoryDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-histories.get";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenHistoryDetailResponse>> HandleAsync(CodegenHistoryIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await queries.FindAsync(request.Id, context).ConfigureAwait(false);
        return found.TryGetValue(out var history) ? CodegenHistoryDetailResponse.From(history) : found.Error!;
    }
}
