// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.AI.Infrastructure.Repositories;

/// <summary>
/// 知识文档仓储实现
/// </summary>
public sealed class KnowledgeDocumentRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysKnowledgeDocument>(clientResolver), IKnowledgeDocumentRepository
{
}
