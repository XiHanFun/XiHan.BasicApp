// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 枚举类型目录：把列配置里的枚举类型名解析为模板可消费的事实
/// </summary>
public interface IEnumTypeCatalog
{
    /// <summary>
    /// 解析枚举类型名
    /// </summary>
    /// <param name="enumTypeName">枚举类型名，全名优先、回退短名，均为 Ordinal 精确匹配</param>
    /// <param name="facts">解析结果</param>
    /// <returns>解析成功返回 true</returns>
    bool TryResolve(string? enumTypeName, out EnumTypeFacts facts);
}

/// <summary>
/// 枚举类型事实
/// </summary>
/// <param name="ShortName">类型短名（与枚举元数据端点的键一致）</param>
/// <param name="Namespace">所在命名空间（限定类型名用）</param>
/// <param name="DefaultMemberName">首个成员名（表单默认值用）</param>
public sealed record EnumTypeFacts(string ShortName, string Namespace, string DefaultMemberName);
