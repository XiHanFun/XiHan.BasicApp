// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Services;
using XiHan.BasicApp.CodeGeneration.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenTemplates;

/// <summary>
/// 模板列表
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-templates:list</c>。列表项不带正文；筛选与排序见 <see cref="CodegenTemplateQueries"/>
/// </remarks>
/// <param name="queries">模板查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TemplatesRead)]
public sealed class ListCodegenTemplates(CodegenTemplateQueries queries) : IOperation<PageRequest, PageResult<CodegenTemplateResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-templates.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<CodegenTemplateResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 模板详情
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-templates/{id}</c>，带正文与路径表达式
/// </remarks>
/// <param name="service">模板规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TemplatesRead)]
public sealed class GetCodegenTemplate(CodeGenTemplateService service) : IOperation<CodegenTemplateIdRequest, CodegenTemplateDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-templates.get";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTemplateDetailResponse>> HandleAsync(CodegenTemplateIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var template) ? CodegenTemplateDetailResponse.From(template) : found.Error!;
    }
}

/// <summary>
/// 检查模板语法
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-templates:validate</c>。只解析不渲染，不写库；要读权限。正文可能很长，走 POST（不声明只读，免得按 ADR-0041 改成 GET 放进查询串）
/// </remarks>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TemplatesRead)]
public sealed class ValidateCodegenTemplate : IOperation<ValidateCodegenTemplateRequest, CodegenTemplateValidationResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-templates.validate";

    /// <inheritdoc />
    public ValueTask<Result<CodegenTemplateValidationResponse>> HandleAsync(ValidateCodegenTemplateRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var errors = CodeGenRenderer.Validate(request.TemplateContent);
        return ValueTask.FromResult(Result.Success(new CodegenTemplateValidationResponse(errors.Count == 0, errors)));
    }
}

/// <summary>
/// 新建模板
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-templates</c>。编码唯一（<c>CODEGENERATION_CONFLICT_005</c>）；新建的都不是内置模板
/// </remarks>
/// <param name="templates">模板仓储</param>
/// <param name="service">模板规则</param>
/// <param name="ids">标识生成器</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TemplatesCreate)]
public sealed class CreateCodegenTemplate(IRepository<SysCodeGenTemplate, Guid> templates, CodeGenTemplateService service, IIdGenerator<Guid> ids) : IOperation<CreateCodegenTemplateRequest, CodegenTemplateDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-templates.create";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTemplateDetailResponse>> HandleAsync(CreateCodegenTemplateRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var code = request.TemplateCode.Trim();
        if (await service.CodeTakenAsync(code, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(CodeGenerationErrors.TemplateCodeTaken, CodeGenerationMessages.TemplateCodeTaken, MessageArguments.Of(("code", code)));
        }

        var template = new SysCodeGenTemplate
        {
            Id = ids.NewId(),
            TemplateCode = code,
            TemplateName = request.TemplateName.Trim(),
            TemplateDescription = CodeGenDataSourceService.Trimmed(request.TemplateDescription),
            TemplateGroup = CodeGenDataSourceService.Trimmed(request.TemplateGroup),
            TemplateType = request.TemplateType,
            TemplateEngine = request.TemplateEngine,
            WriteMode = request.WriteMode,
            TemplateContent = string.IsNullOrWhiteSpace(request.TemplateContent) ? null : request.TemplateContent,
            FileNameExpression = CodeGenDataSourceService.Trimmed(request.FileNameExpression),
            FilePathExpression = CodeGenDataSourceService.Trimmed(request.FilePathExpression),
            FileExtension = CodeGenDataSourceService.Trimmed(request.FileExtension),
            IsBuiltIn = false,
            Status = request.Status,
            Order = request.Order,
            Remark = CodeGenDataSourceService.Trimmed(request.Remark),
        };
        var added = await templates.AddAsync(template, context.CancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : CodegenTemplateDetailResponse.From(template);
    }
}

/// <summary>
/// 修改模板
/// </summary>
/// <remarks>
/// <c>PUT /api/codegen-templates/{id}</c>。内置模板不能改（<c>CODEGENERATION_CONFLICT_006</c>，启动时会按嵌入的文件对齐，改了也会被覆盖）；交回的并发戳与当前的不同即冲突
/// </remarks>
/// <param name="templates">模板仓储</param>
/// <param name="service">模板规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TemplatesUpdate)]
public sealed class UpdateCodegenTemplate(IRepository<SysCodeGenTemplate, Guid> templates, CodeGenTemplateService service) : IOperation<UpdateCodegenTemplateRequest, CodegenTemplateDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-templates.update";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTemplateDetailResponse>> HandleAsync(UpdateCodegenTemplateRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var template))
        {
            return found.Error!;
        }

        if (template.IsBuiltIn)
        {
            return CodeGenTemplateService.BuiltIn(template);
        }

        template.ConcurrencyStamp = request.ConcurrencyStamp;
        template.TemplateName = request.TemplateName.Trim();
        template.TemplateDescription = CodeGenDataSourceService.Trimmed(request.TemplateDescription);
        template.TemplateGroup = CodeGenDataSourceService.Trimmed(request.TemplateGroup);
        template.TemplateType = request.TemplateType;
        template.TemplateEngine = request.TemplateEngine;
        template.WriteMode = request.WriteMode;
        template.TemplateContent = string.IsNullOrWhiteSpace(request.TemplateContent) ? null : request.TemplateContent;
        template.FileNameExpression = CodeGenDataSourceService.Trimmed(request.FileNameExpression);
        template.FilePathExpression = CodeGenDataSourceService.Trimmed(request.FilePathExpression);
        template.FileExtension = CodeGenDataSourceService.Trimmed(request.FileExtension);
        template.Status = request.Status;
        template.Order = request.Order;
        template.Remark = CodeGenDataSourceService.Trimmed(request.Remark);
        var updated = await templates.UpdateAsync(template, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : CodegenTemplateDetailResponse.From(template);
    }
}

/// <summary>
/// 启用或停用模板
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-templates/{id}:set-status</c>。内置模板也能启停（种子对齐时不动启停）；给了备注即替换备注
/// </remarks>
/// <param name="templates">模板仓储</param>
/// <param name="service">模板规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TemplatesUpdate)]
public sealed class SetCodegenTemplateStatus(IRepository<SysCodeGenTemplate, Guid> templates, CodeGenTemplateService service) : IOperation<SetCodegenTemplateStatusRequest, CodegenTemplateDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-templates.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTemplateDetailResponse>> HandleAsync(SetCodegenTemplateStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var template))
        {
            return found.Error!;
        }

        template.ConcurrencyStamp = request.ConcurrencyStamp;
        template.Status = request.Status;
        template.Remark = CodeGenDataSourceService.Trimmed(request.Remark) ?? template.Remark;
        var updated = await templates.UpdateAsync(template, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : CodegenTemplateDetailResponse.From(template);
    }
}

/// <summary>
/// 批量删除模板
/// </summary>
/// <remarks>
/// <c>DELETE /api/codegen-templates?ids=...</c>。逐项：不存在 <c>CODEGENERATION_NOTFOUND_003</c>，内置模板 <c>CODEGENERATION_CONFLICT_006</c>
/// </remarks>
/// <param name="templates">模板仓储</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TemplatesDelete)]
public sealed class DeleteCodegenTemplates(IRepository<SysCodeGenTemplate, Guid> templates) : IOperation<CodegenTemplateIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-templates.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(CodegenTemplateIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = (await templates.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static t => t.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var template))
            {
                failed.Add(new BatchFailure(id, CodeGenerationErrors.TemplateNotFound, CodeGenerationMessages.TemplateNotFound.L(("id", id))));
                continue;
            }

            if (template.IsBuiltIn)
            {
                failed.Add(new BatchFailure(id, CodeGenerationErrors.TemplateBuiltIn, CodeGenerationMessages.TemplateBuiltIn.L(("code", template.TemplateCode))));
                continue;
            }

            var removed = await templates.RemoveAsync(template, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
