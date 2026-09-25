// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.Permissions;

/// <summary>
/// 知识库权限码常量
/// </summary>
/// <remarks>
/// 权限目录 <c>AiPermissionCatalogSeeder</c> 按「资源 × 操作」声明：资源 <c>knowledge_base</c> + 操作 read/create/update/delete/execute，
/// 声明出的码必须与这里一一对应。
/// </remarks>
public static class KnowledgePermissionCodes
{
    /// <summary>资源编码</summary>
    public const string Resource = "knowledge_base";

    /// <summary>查看（文档列表/详情）</summary>
    public const string Read = "knowledge_base:read";

    /// <summary>创建（摄取文档）</summary>
    public const string Create = "knowledge_base:create";

    /// <summary>更新（重建索引）</summary>
    public const string Update = "knowledge_base:update";

    /// <summary>删除</summary>
    public const string Delete = "knowledge_base:delete";

    /// <summary>执行（检索/问答）</summary>
    public const string Execute = "knowledge_base:execute";
}
