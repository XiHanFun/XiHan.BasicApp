#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IKnowledgeDocumentRepository
// Guid:b5b33968-6664-4718-9e79-ff42d7a9d1f0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.AI.Domain.Repositories;

/// <summary>
/// 知识文档仓储接口（基础 CRUD 由 SaasRepository 提供，无需额外查询）
/// </summary>
public interface IKnowledgeDocumentRepository : ISaasRepository<SysKnowledgeDocument>
{
}
