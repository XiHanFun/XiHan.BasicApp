// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.Permissions;

/// <summary>
/// AI 模块权限码常量
/// </summary>
/// <remarks>
/// 权限目录 <c>AiPermissionCatalogSeeder</c> 按「资源 × 操作」声明：资源 <c>ai</c> + 操作 read/create/update/delete/execute，
/// 声明出的码必须与这里一一对应，否则鉴权 403（有测试钉住）。
/// </remarks>
public static class AiPermissionCodes
{
    /// <summary>模块编码（AI 模块全部权限共用）</summary>
    public const string Module = "ai";

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
