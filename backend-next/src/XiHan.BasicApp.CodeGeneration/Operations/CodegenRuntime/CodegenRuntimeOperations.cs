// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.CodeGeneration.Domain.Services;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenRuntime;

/// <summary>
/// 零代码运行时的表结构。
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-runtime/{id}:get-schema</c>。按列配置给出列名、数据行里的键、显示名与控件（照 4.x）；表配置停用即 <c>CODEGENERATION_CONFLICT_007</c>。
/// </remarks>
/// <param name="service">零代码运行时。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = CodeGenerationPermissions.RuntimeRead)]
public sealed class GetCodegenRuntimeSchema(CodeGenRuntimeService service) : IOperation<CodegenRuntimeIdRequest, CodegenRuntimeSchemaResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-runtime.get-schema";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenRuntimeSchemaResponse>> HandleAsync(CodegenRuntimeIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.SchemaAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 零代码运行时的分页数据。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-runtime:list</c>。只读，按表配置的数据源查，每页最多 500 条；数据行的键是列配置的属性名。
/// 主库的表有租户列的只看调用方这一侧的行，有软删列的不看已删的（照 4.x）；库里读不出即 <c>CODEGENERATION_UNAVAILABLE_003</c>。
/// </remarks>
/// <param name="service">零代码运行时。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.RuntimeRead)]
public sealed class ListCodegenRuntime(CodeGenRuntimeService service) : IOperation<ListCodegenRuntimeRequest, PageResult<IReadOnlyDictionary<string, JsonElement>>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-runtime.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<IReadOnlyDictionary<string, JsonElement>>>> HandleAsync(ListCodegenRuntimeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.ListAsync(request, context).ConfigureAwait(false);
    }
}
