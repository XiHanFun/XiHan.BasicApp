// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.Permissions;

/// <summary>
/// AI 助手权限码常量
/// </summary>
/// <remarks>权限目录 <c>AiPermissionCatalogSeeder</c> 声明资源 <c>ai_assistant</c> + 操作 read/create/update/delete，码与这里一一对应，否则鉴权 403。</remarks>
public static class AiAssistantPermissionCodes
{
    /// <summary>资源编码</summary>
    public const string Resource = "ai_assistant";

    /// <summary>查看</summary>
    public const string Read = "ai_assistant:read";

    /// <summary>创建</summary>
    public const string Create = "ai_assistant:create";

    /// <summary>更新</summary>
    public const string Update = "ai_assistant:update";

    /// <summary>删除</summary>
    public const string Delete = "ai_assistant:delete";
}
