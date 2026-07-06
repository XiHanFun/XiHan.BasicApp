#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiPermissionCodes
// Guid:6fc08b39-c079-44f5-a6ea-1ab2bd30d8eb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.AI.Domain.Permissions;

/// <summary>
/// AI 模块权限码常量
/// </summary>
/// <remarks>
/// 与种子数据 <c>SysPermissionSeeder</c> 对齐：资源 <c>ai</c> + 操作 read/create/update/delete/execute。
/// 三处（本常量、<c>SysResourceSeeder.ResourceCode</c>、<c>SysPermissionSeeder</c> 目标字典）必须一致，否则鉴权 403。
/// </remarks>
public static class AiPermissionCodes
{
    /// <summary>资源编码</summary>
    public const string Resource = "ai";

    /// <summary>查看（列表/详情）</summary>
    public const string Read = "ai:read";

    /// <summary>创建（新增 provider 配置）</summary>
    public const string Create = "ai:create";

    /// <summary>更新（含设为默认、启停）</summary>
    public const string Update = "ai:update";

    /// <summary>删除</summary>
    public const string Delete = "ai:delete";

    /// <summary>执行（测试连接、调用推理）</summary>
    public const string Execute = "ai:execute";
}
