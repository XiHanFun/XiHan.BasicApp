#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:QueryConditionsExtensions
// Guid:3a7e2c91-f84b-4d05-b6e8-1c9f0a5d7b43
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.Linq.Expressions;
using System.Reflection;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.Extensions;

/// <summary>
/// <see cref="QueryConditions"/> 类型安全扩展方法。
/// 通过 Lambda 表达式选择器在编译期确定字段名，避免裸字符串因属性重命名而悄然失效。
/// </summary>
public static class QueryConditionsExtensions
{
    /// <summary>
    /// 以表达式选择器添加单值过滤条件（编译期校验字段存在）。
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    /// <param name="conditions">查询条件对象</param>
    /// <param name="selector">字段选择器，例如 <c>u => u.Status</c></param>
    /// <param name="value">过滤值</param>
    /// <param name="operator">比较操作符，默认为等于</param>
    /// <returns>当前 <see cref="QueryConditions"/> 实例（支持链式调用）</returns>
    public static QueryConditions AddFilter<T, TValue>(
        this QueryConditions conditions,
        Expression<Func<T, TValue>> selector,
        TValue value,
        QueryOperator @operator = QueryOperator.Equal)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(selector);

        var field = GetMemberName(selector);
        return conditions.AddFilter(field, (object?)value, @operator);
    }

    /// <summary>
    /// 以表达式选择器添加多值 In 过滤条件（编译期校验字段存在）。
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    /// <param name="conditions">查询条件对象</param>
    /// <param name="selector">字段选择器，例如 <c>u => u.Status</c></param>
    /// <param name="values">候选值集合</param>
    /// <param name="operator">比较操作符，默认为 <see cref="QueryOperator.In"/></param>
    /// <returns>当前 <see cref="QueryConditions"/> 实例（支持链式调用）</returns>
    public static QueryConditions AddFilterIn<T, TValue>(
        this QueryConditions conditions,
        Expression<Func<T, TValue>> selector,
        IEnumerable<object> values,
        QueryOperator @operator = QueryOperator.In)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(values);

        var field = GetMemberName(selector);
        var filter = new QueryFilter(field, values.ToArray(), @operator);
        return conditions.AddFilter(filter);
    }

    /// <summary>
    /// 以表达式选择器添加排序条件（编译期校验字段存在）。
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    /// <param name="conditions">查询条件对象</param>
    /// <param name="selector">字段选择器，例如 <c>u => u.CreatedTime</c></param>
    /// <param name="direction">排序方向，默认为升序</param>
    /// <param name="priority">排序优先级，数值越小优先级越高，默认为 0</param>
    /// <returns>当前 <see cref="QueryConditions"/> 实例（支持链式调用）</returns>
    public static QueryConditions AddSort<T, TValue>(
        this QueryConditions conditions,
        Expression<Func<T, TValue>> selector,
        SortDirection direction = SortDirection.Ascending,
        int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(selector);

        var field = GetMemberName(selector);
        return conditions.AddSort(field, direction, priority);
    }

    /// <summary>
    /// 以表达式选择器数组设置关键字搜索字段（编译期校验字段存在）。
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="conditions">查询条件对象</param>
    /// <param name="keyword">搜索关键字</param>
    /// <param name="selectors">
    /// 字段选择器数组，例如
    /// <c>u => u.UserName, u => u.RealName</c>。
    /// 选择器返回值统一声明为 <c>object?</c> 以兼容引用类型与值类型属性。
    /// </param>
    /// <returns>当前 <see cref="QueryConditions"/> 实例（支持链式调用）</returns>
    public static QueryConditions SetKeyword<T>(
        this QueryConditions conditions,
        string keyword,
        params Expression<Func<T, object?>>[] selectors)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(selectors);

        var fields = selectors.Select(s => GetMemberName(s)).ToArray();
        return conditions.SetKeyword(keyword, fields);
    }

    /// <summary>
    /// 从成员访问表达式中解析属性或字段名称。
    /// <para>
    /// 支持两种常见形式：
    /// <list type="bullet">
    /// <item><description><c>u => u.UserName</c>（引用类型，直接为 <see cref="MemberExpression"/>）</description></item>
    /// <item><description><c>u => u.Status</c>（值类型，编译器会在外层包裹 <c>Convert</c> 装箱为 <see langword="object"/>，
    /// 即 <see cref="UnaryExpression"/>）</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TValue">字段值类型</typeparam>
    /// <param name="selector">字段选择器</param>
    /// <returns>成员名称字符串</returns>
    /// <exception cref="ArgumentException">表达式不是成员访问表达式时抛出</exception>
    private static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> selector)
    {
        var body = selector.Body;

        // 值类型属性（如 enum、int、bool）被编译器隐式 Convert 装箱为 object：
        // 表达式树形如 Convert(u.Status, Object)，需剥去外层 UnaryExpression。
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression member)
        {
            EnsureQueryableColumn(member.Member, selector);
            return member.Member.Name;
        }

        throw new ArgumentException(
            $"选择器必须是简单的成员访问表达式（如 u => u.PropertyName），实际收到：{selector}",
            nameof(selector));
    }

    /// <summary>
    /// 校验所选成员可作为数据库列参与查询。
    /// <para>
    /// 标注了 <c>[SugarColumn(IsIgnore = true)]</c> 的成员是<b>派生/导航属性</b>（如 <c>SysPermission.IsGlobal =&gt; TenantId == 0</c>），
    /// 不落库；若误用于 <c>AddFilter</c>/<c>AddSort</c>/<c>SetKeyword</c> 会被翻译成不存在的列，导致运行期 SQL 报错（如 PostgreSQL 42703）。
    /// 此处提前在构建查询条件时抛出清晰异常，将该类错误从"线上运行期"前移到"开发期"。
    /// </para>
    /// </summary>
    /// <param name="member">表达式解析出的成员</param>
    /// <param name="selector">原始选择器（用于异常信息定位）</param>
    /// <exception cref="ArgumentException">成员为不落库的派生/导航属性时抛出</exception>
    private static void EnsureQueryableColumn(MemberInfo member, Expression selector)
    {
        var sugarColumn = member.GetCustomAttribute<SugarColumn>();
        if (sugarColumn is { IsIgnore: true })
        {
            throw new ArgumentException(
                $"成员 {member.DeclaringType?.Name}.{member.Name} 标注了 [SugarColumn(IsIgnore = true)]，"
                + $"属于不落库的派生/导航属性，不能用于数据库查询过滤/排序/关键字。"
                + $"请改用其对应的真实列（如 IsGlobal 应改用 TenantId == 0）。原始选择器：{selector}",
                nameof(selector));
        }
    }
}
