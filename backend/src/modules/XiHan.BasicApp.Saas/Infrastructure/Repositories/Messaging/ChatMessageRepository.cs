#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatMessageRepository
// Guid:8d2e1f4a-6fd1-4ef1-b720-1c2d3e4f5a6b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 聊天消息仓储实现
/// </summary>
public sealed class ChatMessageRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysChatMessage>(clientResolver), IChatMessageRepository;
