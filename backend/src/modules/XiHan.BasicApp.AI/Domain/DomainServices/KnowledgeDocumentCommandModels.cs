#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgeDocumentCommandModels
// Guid:a11c0de0-5005-4a10-9a00-00000000ai54
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Enums;

namespace XiHan.BasicApp.AI.Domain.DomainServices;

/// <summary>
/// 知识文档摄取命令
/// </summary>
public sealed record KnowledgeIngestCommand(
    string Title,
    KnowledgeSourceType SourceType,
    string? Source,
    string Text,
    string? EmbeddingProviderCode,
    string? Remark);

/// <summary>
/// 知识文档命令结果
/// </summary>
public sealed record KnowledgeDocumentCommandResult(SysKnowledgeDocument Document);
