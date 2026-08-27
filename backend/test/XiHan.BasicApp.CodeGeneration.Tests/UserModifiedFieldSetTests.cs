// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 已人工修改字段集合（dirty-tracking）测试。
/// </summary>
/// <remarks>
/// 这是"人工配置不被同步表结构冲掉"的最后一道闸：集合内字段在同步时冻结、集合外字段跟随重新推断。
/// 解析失败必须安全降级为空集（退化成全部重新推断），绝不能抛异常把整条同步链打断；
/// 比对必须精准（未变化字段不得入集合），否则冻结面越滚越大、同步表结构彻底失效。
/// </remarks>
public sealed class UserModifiedFieldSetTests
{
    /// <summary>
    /// 合法 JSON 字符串数组正常解析出全部字段名。
    /// </summary>
    [Fact]
    public void Parse_ValidJsonShouldReturnAllNames()
    {
        var set = UserModifiedFieldSet.Parse("[\"ClassName\",\"Namespace\"]");

        Assert.Equal(2, set.Count);
        Assert.Contains("ClassName", set, StringComparer.Ordinal);
        Assert.Contains("Namespace", set, StringComparer.Ordinal);
    }

    /// <summary>
    /// 脏数据一律安全降级为空集而不是抛异常：非法 JSON、对象、数字数组、嵌套数组都算脏数据。
    /// </summary>
    /// <param name="json">脏 JSON 文本</param>
    [Theory]
    [InlineData("{}")]
    [InlineData("[1,2]")]
    [InlineData("[[\"a\"]]")]
    [InlineData("not-json")]
    [InlineData("[\"a\",")]
    [InlineData("123")]
    [InlineData("true")]
    public void Parse_DirtyJsonShouldDegradeToEmptySet(string json)
    {
        Assert.Empty(UserModifiedFieldSet.Parse(json));
    }

    /// <summary>
    /// null / 空 / 纯空白 / JSON null 都返回空集，同样不抛异常。
    /// </summary>
    /// <param name="json">空值或空白 JSON</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    public void Parse_NullOrBlankShouldReturnEmptySet(string? json)
    {
        Assert.Empty(UserModifiedFieldSet.Parse(json));
    }

    /// <summary>
    /// 解析结果大小写不敏感，且元素会被 Trim、空白元素被丢弃；
    /// 否则前端传来的 " ClassName " 会因为多了空格而让冻结失效。
    /// </summary>
    [Fact]
    public void Parse_ShouldTrimElementsAndIgnoreCase()
    {
        var set = UserModifiedFieldSet.Parse("[\" ClassName \",\"\",\"   \",null]");

        Assert.Equal("ClassName", Assert.Single(set), StringComparer.Ordinal);
        Assert.True(set.Contains("classname"), "集合本身必须大小写不敏感");
        Assert.True(set.Contains("CLASSNAME"), "集合本身必须大小写不敏感");
    }

    /// <summary>
    /// 空集序列化为 null 而不是 "[]"：避免落库空数组、也让"从未人工改过"与"改过又全恢复"在库里同形。
    /// </summary>
    [Fact]
    public void Serialize_EmptyShouldReturnNull()
    {
        Assert.Null(UserModifiedFieldSet.Serialize([]));
    }

    /// <summary>
    /// 非空集合按 Ordinal 排序输出，保证同一集合任意插入顺序都得到逐字相同的 JSON，
    /// 便于快照比对与"无变化不写库"的判定。
    /// </summary>
    [Fact]
    public void Serialize_ShouldBeOrdinalSortedAndStable()
    {
        var first = UserModifiedFieldSet.Serialize(["Namespace", "Author", "ClassName"]);
        var second = UserModifiedFieldSet.Serialize(["ClassName", "Namespace", "Author"]);

        Assert.Equal("[\"Author\",\"ClassName\",\"Namespace\"]", first, StringComparer.Ordinal);
        Assert.Equal(first, second, StringComparer.Ordinal);
    }

    /// <summary>
    /// 合并只增不减：已有字段保留，新增字段并入。
    /// </summary>
    [Fact]
    public void Merge_ShouldAddNewFieldsAndKeepExisting()
    {
        var merged = UserModifiedFieldSet.Merge("[\"ClassName\"]", ["Namespace"]);

        var set = UserModifiedFieldSet.Parse(merged);
        Assert.Equal(2, set.Count);
        Assert.Contains("ClassName", set, StringComparer.Ordinal);
        Assert.Contains("Namespace", set, StringComparer.Ordinal);
    }

    /// <summary>
    /// 没有真正新增时必须原样返回入参 JSON（同一引用，不重排也不改写），
    /// 否则每次保存都会产生一次无谓的 UPDATE 与审计噪声。
    /// </summary>
    /// <param name="changedJoined">本次变化字段（以 <c>|</c> 连接；空串表示无变化字段）</param>
    [Theory]
    [InlineData("")]
    [InlineData("ClassName")]
    [InlineData("classname")]
    [InlineData(" CLASSNAME ")]
    [InlineData("| ")]
    [InlineData("ClassName|CLASSNAME|className")]
    public void Merge_NoNewFieldShouldReturnOriginalJsonUnchanged(string changedJoined)
    {
        const string Existing = "[\"ClassName\"]";
        string[] changed = changedJoined.Length == 0 ? [] : changedJoined.Split('|');

        Assert.Same(Existing, UserModifiedFieldSet.Merge(Existing, changed));
    }

    /// <summary>
    /// 原集合为空且无新增时返回 null（保持"未落库"状态）。
    /// </summary>
    [Fact]
    public void Merge_EmptyExistingAndNoChangeShouldReturnNull()
    {
        Assert.Null(UserModifiedFieldSet.Merge(null, []));
        Assert.Null(UserModifiedFieldSet.Merge(null, ["", "  "]));
    }

    /// <summary>
    /// 大小写不同的同名字段视为同一字段，不产生重复项。
    /// </summary>
    [Fact]
    public void Merge_CaseInsensitiveDuplicateShouldNotGrowSet()
    {
        var merged = UserModifiedFieldSet.Merge("[\"ClassName\"]", ["CLASSNAME", "className", "Author"]);

        var set = UserModifiedFieldSet.Parse(merged);
        Assert.Equal(2, set.Count);
        Assert.Contains("Author", set, StringComparer.Ordinal);
    }

    /// <summary>
    /// 脏数据基线上的合并只会保留本次新增（脏数据已被降级成空集），不抛异常。
    /// </summary>
    [Fact]
    public void Merge_DirtyExistingJsonShouldRebuildFromChangedOnly()
    {
        var merged = UserModifiedFieldSet.Merge("{bad", ["Author"]);

        Assert.Equal("[\"Author\"]", merged, StringComparer.Ordinal);
    }

    /// <summary>
    /// 移除命中时重新序列化；移除最后一项后返回 null，恢复"全部跟随推断"。
    /// </summary>
    [Fact]
    public void Remove_ShouldReserializeAndReturnNullWhenSetBecomesEmpty()
    {
        Assert.Equal("[\"Author\"]", UserModifiedFieldSet.Remove("[\"Author\",\"ClassName\"]", "ClassName"), StringComparer.Ordinal);
        Assert.Null(UserModifiedFieldSet.Remove("[\"ClassName\"]", "ClassName"));
        Assert.Null(UserModifiedFieldSet.Remove("[\"ClassName\"]", "CLASSNAME"));
    }

    /// <summary>
    /// 未命中时原样返回入参 JSON（同一引用），避免无谓写库。
    /// </summary>
    [Fact]
    public void Remove_MissShouldReturnOriginalJsonUnchanged()
    {
        const string Existing = "[\"ClassName\"]";

        Assert.Same(Existing, UserModifiedFieldSet.Remove(Existing, "Author"));
        Assert.Null(UserModifiedFieldSet.Remove(null, "Author"));
    }

    /// <summary>
    /// 包含判定大小写不敏感；json 为空时一律返回 false（未冻结）。
    /// </summary>
    /// <param name="json">集合 JSON</param>
    /// <param name="fieldName">待判定字段名</param>
    /// <param name="expected">期望结果</param>
    [Theory]
    [InlineData("[\"ClassName\"]", "ClassName", true)]
    [InlineData("[\"ClassName\"]", "classname", true)]
    [InlineData("[\"ClassName\"]", "CLASSNAME", true)]
    [InlineData("[\"ClassName\"]", "Namespace", false)]
    [InlineData(null, "ClassName", false)]
    [InlineData("", "ClassName", false)]
    [InlineData("{bad", "ClassName", false)]
    public void Contains_ShouldIgnoreCaseAndFailClosedOnBlankJson(string? json, string fieldName, bool expected)
    {
        Assert.Equal(expected, UserModifiedFieldSet.Contains(json, fieldName));
    }

    /// <summary>
    /// 快照按属性名反射取值；缺失属性记 null 而不抛异常（字段清单里写了实体上没有的名字时安全降级）。
    /// </summary>
    [Fact]
    public void Snapshot_ShouldReadPropertiesAndRecordMissingAsNull()
    {
        var entity = new TrackedEntity { ClassName = "SysUser", Sort = 3, Enabled = true, Kind = SampleKind.Second };

        var snapshot = UserModifiedFieldSet.Snapshot(entity, ["ClassName", "Sort", "Enabled", "Kind", "NotExists"]);

        Assert.Equal(5, snapshot.Count);
        Assert.Equal("SysUser", snapshot["ClassName"]);
        Assert.Equal(3, snapshot["Sort"]);
        Assert.Equal(true, snapshot["Enabled"]);
        Assert.Equal(SampleKind.Second, snapshot["Kind"]);
        Assert.Null(snapshot["NotExists"]);
    }

    /// <summary>
    /// 快照键为 Ordinal 精确匹配：属性名大小写写错取不到值，记 null。
    /// </summary>
    [Fact]
    public void Snapshot_KeyShouldBeOrdinalExact()
    {
        var snapshot = UserModifiedFieldSet.Snapshot(new TrackedEntity { ClassName = "SysUser" }, ["classname"]);

        Assert.Null(snapshot["classname"]);
        Assert.True(snapshot.ContainsKey("classname"));
        Assert.False(snapshot.ContainsKey("ClassName"));
    }

    /// <summary>
    /// 未变化的字段绝不能进入 diff 结果——多记一个字段就等于永久冻结一列的自动推断。
    /// </summary>
    [Fact]
    public void DiffChanged_UnchangedEntityShouldReturnEmpty()
    {
        var entity = new TrackedEntity { ClassName = "SysUser", Sort = 1, Enabled = false, Kind = SampleKind.First };
        var before = UserModifiedFieldSet.Snapshot(entity, ["ClassName", "Sort", "Enabled", "Kind", "Optional"]);

        Assert.Empty(UserModifiedFieldSet.DiffChanged(entity, before));
    }

    /// <summary>
    /// 值类型/可空/枚举/字符串各类字段的变化都必须被识别，且只报真正变化的那些。
    /// </summary>
    [Fact]
    public void DiffChanged_ShouldDetectStringValueNullableAndEnumChanges()
    {
        var entity = new TrackedEntity { ClassName = "SysUser", Sort = 1, Enabled = false, Kind = SampleKind.First, Optional = null };
        var before = UserModifiedFieldSet.Snapshot(entity, ["ClassName", "Sort", "Enabled", "Kind", "Optional"]);

        entity.ClassName = "SysAccount";
        entity.Sort = 2;
        entity.Kind = SampleKind.Second;
        entity.Optional = 9;

        var changed = UserModifiedFieldSet.DiffChanged(entity, before);

        Assert.Equal(4, changed.Count);
        Assert.Contains("ClassName", changed, StringComparer.Ordinal);
        Assert.Contains("Sort", changed, StringComparer.Ordinal);
        Assert.Contains("Kind", changed, StringComparer.Ordinal);
        Assert.Contains("Optional", changed, StringComparer.Ordinal);
        Assert.DoesNotContain("Enabled", changed, StringComparer.Ordinal);
    }

    /// <summary>
    /// 字符串比较是值相等而非引用相等：内容相同的两个实例不算变化。
    /// </summary>
    [Fact]
    public void DiffChanged_SameValueDifferentInstanceShouldNotCount()
    {
        var entity = new TrackedEntity { ClassName = "SysUser" };
        var before = UserModifiedFieldSet.Snapshot(entity, ["ClassName"]);

        entity.ClassName = new StringBuilder("Sys").Append("User").ToString();

        Assert.Empty(UserModifiedFieldSet.DiffChanged(entity, before));
    }

    /// <summary>
    /// 从有值改为 null（人工清空备注这类操作）同样算变化，必须被冻结记录。
    /// </summary>
    [Fact]
    public void DiffChanged_ValueClearedToNullShouldCount()
    {
        var entity = new TrackedEntity { Optional = 5 };
        var before = UserModifiedFieldSet.Snapshot(entity, ["Optional"]);

        entity.Optional = null;

        Assert.Equal("Optional", Assert.Single(UserModifiedFieldSet.DiffChanged(entity, before)), StringComparer.Ordinal);
    }

    /// <summary>
    /// 快照 + diff + merge 的完整往返：只有真正改动的字段被并入冻结集合。
    /// </summary>
    [Fact]
    public void SnapshotDiffMerge_RoundTripShouldFreezeOnlyChangedFields()
    {
        var entity = new TrackedEntity { ClassName = "SysUser", Sort = 1 };
        var before = UserModifiedFieldSet.Snapshot(entity, ["ClassName", "Sort"]);
        entity.ClassName = "SysAccount";

        var json = UserModifiedFieldSet.Merge(null, UserModifiedFieldSet.DiffChanged(entity, before));

        Assert.Equal("[\"ClassName\"]", json, StringComparer.Ordinal);
        Assert.True(UserModifiedFieldSet.Contains(json, "ClassName"));
        Assert.False(UserModifiedFieldSet.Contains(json, "Sort"));
    }

    /// <summary>
    /// 测试用实体：覆盖字符串 / 值类型 / 可空值类型 / 枚举四类字段。
    /// </summary>
    private sealed class TrackedEntity
    {
        /// <summary>字符串字段</summary>
        public string? ClassName { get; set; }

        /// <summary>值类型字段</summary>
        public int Sort { get; set; }

        /// <summary>布尔字段</summary>
        public bool Enabled { get; set; }

        /// <summary>枚举字段</summary>
        public SampleKind Kind { get; set; }

        /// <summary>可空值类型字段</summary>
        public int? Optional { get; set; }
    }

    /// <summary>
    /// 测试用枚举。
    /// </summary>
    private enum SampleKind
    {
        /// <summary>第一项</summary>
        First = 0,

        /// <summary>第二项</summary>
        Second = 1
    }
}
