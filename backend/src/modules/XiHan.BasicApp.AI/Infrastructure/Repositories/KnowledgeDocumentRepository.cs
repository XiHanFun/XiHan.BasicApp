#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgeDocumentRepository
// Guid:a11c0de0-5004-4a10-9a00-00000000ai53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
