#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiPromptPermissionCodes
// Guid:a11c0de0-9004-4a10-9a00-00000000ai93
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/06 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
