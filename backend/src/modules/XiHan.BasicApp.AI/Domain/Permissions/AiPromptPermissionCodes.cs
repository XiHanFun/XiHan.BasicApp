// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.Permissions;

/// <summary>
/// AI 提示词库权限码常量
/// </summary>
/// <remarks>与种子对齐：资源 <c>ai_prompt</c> + 操作 read/create/update/delete。三处一致否则鉴权 403。</remarks>
public static class AiPromptPermissionCodes
{
    /// <summary>资源编码</summary>
    public const string Resource = "ai_prompt";

    /// <summary>查看</summary>
    public const string Read = "ai_prompt:read";

    /// <summary>创建</summary>
    public const string Create = "ai_prompt:create";

    /// <summary>更新</summary>
    public const string Update = "ai_prompt:update";

    /// <summary>删除</summary>
    public const string Delete = "ai_prompt:delete";
}
