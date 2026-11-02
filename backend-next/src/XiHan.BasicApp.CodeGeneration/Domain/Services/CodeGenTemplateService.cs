// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 模板的规则。
/// </summary>
/// <remarks>
/// 照 4.x：编码唯一、建好后不能改；内置模板随程序版本走（启动时由种子按嵌入的文件对齐），不能修改也不能删除，只能启停，要定制就新建一份自有模板。
/// </remarks>
/// <param name="templates">模板仓储。</param>
public sealed class CodeGenTemplateService(IRepository<SysCodeGenTemplate, Guid> templates)
{
    /// <summary>
    /// 不存在。
    /// </summary>
    /// <param name="id">模板 ID。</param>
    public static Error NotFound(Guid id)
        => Error.NotFound(CodeGenerationErrors.TemplateNotFound, CodeGenerationMessages.TemplateNotFound, MessageArguments.Of(("id", id)));

    /// <summary>
    /// 内置模板不能改、不能删。
    /// </summary>
    /// <param name="template">模板。</param>
    public static Error BuiltIn(SysCodeGenTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Error.Conflict(CodeGenerationErrors.TemplateBuiltIn, CodeGenerationMessages.TemplateBuiltIn, MessageArguments.Of(("code", template.TemplateCode)));
    }

    /// <summary>
    /// 按 ID 取模板。
    /// </summary>
    /// <param name="id">模板 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result<SysCodeGenTemplate>> FindAsync(Guid id, CancellationToken cancellationToken)
        => (await templates.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var template) ? template : NotFound(id);

    /// <summary>
    /// 编码是否已被占用。
    /// </summary>
    /// <param name="code">编码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<bool> CodeTakenAsync(string code, CancellationToken cancellationToken)
        => await templates.CountAsync(EntityQuery.All<SysCodeGenTemplate>().Where(new Specification<SysCodeGenTemplate>(t => t.TemplateCode == code)), cancellationToken).ConfigureAwait(false) > 0;
}
