// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.AI.Domain.Repositories;

/// <summary>
/// 知识文档仓储接口（基础 CRUD 由 SaasRepository 提供，无需额外查询）
/// </summary>
public interface IKnowledgeDocumentRepository : ISaasRepository<SysKnowledgeDocument>
{
}
