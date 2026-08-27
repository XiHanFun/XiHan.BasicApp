// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Core.Tests;

/// <summary>
/// BasicApp 实体基类家族的 SqlSugar 列特性测试。
/// </summary>
/// <remarks>
/// 列特性是"实体形状"与"数据库形状"之间唯一的桥。它上面的每个布尔位都对应一条真实后果：
/// <list type="bullet">
/// <item><c>IsOnlyIgnoreUpdate</c> 决定这列会不会出现在 UPDATE 语句里——创建审计列丢了它就会被覆盖，
/// 而软删列若误加上它，删除（本质是 UPDATE）永远写不进库；</item>
/// <item><c>IsEnableUpdateVersionValidation</c> 是乐观并发的总开关，丢失后并发更新互相覆盖且不抛异常；</item>
/// <item><c>IsIdentity</c> 决定主键由分布式 Id 生成器还是数据库自增产生，改错会撞主键或被库覆盖；</item>
/// <item><c>ColumnName</c> 就是线上列名，改动等于所有手写升级脚本 SQL 立即失效。</item>
/// </list>
/// </remarks>
public sealed class BasicAppEntitySugarColumnTests
{
    /// <summary>
    /// 使用 snake_case 显式列名的六个"实体家族"基类（聚合根不在其列，见文件末尾）。
    /// </summary>
    public static TheoryData<Type> EntityFamilyBaseTypes =>
    [
        typeof(BasicAppEntity),
        typeof(BasicAppEntityWithIdentity),
        typeof(BasicAppCreationEntity),
        typeof(BasicAppModificationEntity),
        typeof(BasicAppDeletionEntity),
        typeof(BasicAppFullAuditedEntity)
    ];

    /// <summary>
    /// 具备完整审计列的两个基类。
    /// </summary>
    public static TheoryData<Type> FullAuditedBaseTypes =>
    [
        typeof(BasicAppFullAuditedEntity),
        typeof(BasicAppAggregateRoot)
    ];

    /// <summary>
    /// 实体家族的主键列必须是 Basic_Id 且为主键。
    /// </summary>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [MemberData(nameof(EntityFamilyBaseTypes))]
    public void BasicId_EntityFamilyShouldMapToBasicIdPrimaryKeyColumn(Type baseType)
    {
        var column = CoreTestHelper.RequireSugarColumn(baseType, "BasicId");

        Assert.Equal("Basic_Id", column.ColumnName, StringComparer.Ordinal);
        Assert.True(column.IsPrimaryKey, $"{baseType.Name}.BasicId 必须是主键。");
    }

    /// <summary>
    /// 非自增主键基类的 IsIdentity 必须为 false，主键由分布式 Id 生成器写入。
    /// </summary>
    /// <remarks>
    /// 一旦变成 true，生成器写入的雪花主键会被数据库自增值覆盖，业务侧持有的 Id 全部对不上库里的行。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [InlineData(typeof(BasicAppEntity))]
    [InlineData(typeof(BasicAppCreationEntity))]
    [InlineData(typeof(BasicAppModificationEntity))]
    [InlineData(typeof(BasicAppDeletionEntity))]
    [InlineData(typeof(BasicAppFullAuditedEntity))]
    [InlineData(typeof(BasicAppAggregateRoot))]
    public void BasicId_NonIdentityBaseTypesShouldDisableIdentity(Type baseType)
    {
        var column = CoreTestHelper.RequireSugarColumn(baseType, "BasicId");

        Assert.True(column.IsPrimaryKey);
        Assert.False(column.IsIdentity, $"{baseType.Name}.BasicId 不得启用自增，主键由分布式 Id 生成器产出。");
    }

    /// <summary>
    /// 自增主键基类的 IsIdentity 必须为 true —— 这是它与 <see cref="BasicAppEntity"/> 的唯一实质差别。
    /// </summary>
    /// <remarks>
    /// 退化成 false 后插入时主键为 0，第二条记录立即触发主键冲突。
    /// </remarks>
    [Fact]
    public void BasicId_IdentityBaseTypeShouldEnableIdentity()
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppEntityWithIdentity), "BasicId");

        Assert.Equal("Basic_Id", column.ColumnName, StringComparer.Ordinal);
        Assert.True(column.IsPrimaryKey);
        Assert.True(column.IsIdentity, "自增主键基类的 IsIdentity 必须为 true。");
    }

    /// <summary>
    /// 七个基类的 RowVersion 都必须映射到 Row_Version 并开启乐观并发校验。
    /// </summary>
    /// <remarks>
    /// 这是全家族唯一一处连聚合根都显式指定了列名的地方；标志丢失后并发更新会互相覆盖且无任何异常。
    /// </remarks>
    /// <param name="baseType">被检查的实体基类。</param>
    [Theory]
    [InlineData(typeof(BasicAppEntity))]
    [InlineData(typeof(BasicAppEntityWithIdentity))]
    [InlineData(typeof(BasicAppCreationEntity))]
    [InlineData(typeof(BasicAppModificationEntity))]
    [InlineData(typeof(BasicAppDeletionEntity))]
    [InlineData(typeof(BasicAppFullAuditedEntity))]
    [InlineData(typeof(BasicAppAggregateRoot))]
    public void RowVersion_ShouldEnableUpdateVersionValidation(Type baseType)
    {
        var column = CoreTestHelper.RequireSugarColumn(baseType, "RowVersion");

        Assert.Equal("Row_Version", column.ColumnName, StringComparer.Ordinal);
        Assert.True(
            column.IsEnableUpdateVersionValidation,
            $"{baseType.Name}.RowVersion 关闭了并发校验，并发更新会静默互相覆盖。");
    }

    /// <summary>
    /// 创建审计三列必须在 UPDATE 中被忽略。
    /// </summary>
    /// <remarks>
    /// 丢失 <c>IsOnlyIgnoreUpdate</c> 后，任何一次更新都会把创建人/创建时间改写成当前值，
    /// 审计链条直接断掉且不可恢复。
    /// </remarks>
    /// <param name="propertyName">创建审计属性名。</param>
    /// <param name="expectedColumnName">期望的列名。</param>
    /// <param name="expectedNullable">该列是否允许 NULL。</param>
    [Theory]
    [InlineData("CreatedTime", "Created_Time", false)]
    [InlineData("CreatedId", "Created_Id", true)]
    [InlineData("CreatedBy", "Created_By", true)]
    public void CreationColumns_ShouldBeIgnoredOnUpdate(string propertyName, string expectedColumnName, bool expectedNullable)
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppCreationEntity), propertyName);

        Assert.Equal(expectedColumnName, column.ColumnName, StringComparer.Ordinal);
        Assert.Equal(expectedNullable, column.IsNullable);
        Assert.True(column.IsOnlyIgnoreUpdate, $"{propertyName} 必须在 UPDATE 中被忽略，否则创建审计会被覆盖。");
    }

    /// <summary>
    /// 完整审计实体的创建三列同样必须在 UPDATE 中被忽略。
    /// </summary>
    /// <param name="propertyName">创建审计属性名。</param>
    /// <param name="expectedColumnName">期望的列名。</param>
    [Theory]
    [InlineData("CreatedTime", "Created_Time")]
    [InlineData("CreatedId", "Created_Id")]
    [InlineData("CreatedBy", "Created_By")]
    public void FullAuditedCreationColumns_ShouldBeIgnoredOnUpdate(string propertyName, string expectedColumnName)
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppFullAuditedEntity), propertyName);

        Assert.Equal(expectedColumnName, column.ColumnName, StringComparer.Ordinal);
        Assert.True(column.IsOnlyIgnoreUpdate);
    }

    /// <summary>
    /// 修改审计三列必须允许 NULL，且**不得**带 IsOnlyIgnoreUpdate。
    /// </summary>
    /// <remarks>
    /// 修改列天生就该在 UPDATE 中被写入；一旦误加上"更新时忽略"，修改时间永远停在 NULL，
    /// 审计上会呈现"这条记录从未被改过"的假象。
    /// </remarks>
    /// <param name="propertyName">修改审计属性名。</param>
    /// <param name="expectedColumnName">期望的列名。</param>
    [Theory]
    [InlineData("ModifiedTime", "Modified_Time")]
    [InlineData("ModifiedId", "Modified_Id")]
    [InlineData("ModifiedBy", "Modified_By")]
    public void ModificationColumns_ShouldRemainUpdatable(string propertyName, string expectedColumnName)
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppModificationEntity), propertyName);

        Assert.Equal(expectedColumnName, column.ColumnName, StringComparer.Ordinal);
        Assert.True(column.IsNullable);
        Assert.False(column.IsOnlyIgnoreUpdate, $"{propertyName} 不得在 UPDATE 中被忽略，否则修改审计永远写不进去。");
    }

    /// <summary>
    /// 完整审计实体的修改三列同样必须保持可更新。
    /// </summary>
    /// <param name="propertyName">修改审计属性名。</param>
    /// <param name="expectedColumnName">期望的列名。</param>
    [Theory]
    [InlineData("ModifiedTime", "Modified_Time")]
    [InlineData("ModifiedId", "Modified_Id")]
    [InlineData("ModifiedBy", "Modified_By")]
    public void FullAuditedModificationColumns_ShouldRemainUpdatable(string propertyName, string expectedColumnName)
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppFullAuditedEntity), propertyName);

        Assert.Equal(expectedColumnName, column.ColumnName, StringComparer.Ordinal);
        Assert.True(column.IsNullable);
        Assert.False(column.IsOnlyIgnoreUpdate);
    }

    /// <summary>
    /// 软删标记列必须是 Is_Deleted、非空且可更新。
    /// </summary>
    /// <remarks>
    /// 软删操作在库里就是一次 UPDATE，因此 Is_Deleted 绝不能带 IsOnlyIgnoreUpdate；
    /// 非空则是"软删唯一索引末列附加 IsDeleted"这条约定成立的前提。
    /// </remarks>
    /// <param name="baseType">支持软删的实体基类。</param>
    [Theory]
    [InlineData(typeof(BasicAppDeletionEntity))]
    [InlineData(typeof(BasicAppFullAuditedEntity))]
    public void IsDeletedColumn_ShouldBeNonNullableAndUpdatable(Type baseType)
    {
        var column = CoreTestHelper.RequireSugarColumn(baseType, "IsDeleted");

        Assert.Equal("Is_Deleted", column.ColumnName, StringComparer.Ordinal);
        Assert.False(column.IsNullable, "Is_Deleted 可空会引入 NULL 第三态，唯一索引与查询口径同时失效。");
        Assert.False(column.IsOnlyIgnoreUpdate, "Is_Deleted 必须可更新，否则软删永远写不进库。");
    }

    /// <summary>
    /// 删除审计三列必须允许 NULL 且保持可更新。
    /// </summary>
    /// <param name="propertyName">删除审计属性名。</param>
    /// <param name="expectedColumnName">期望的列名。</param>
    [Theory]
    [InlineData("DeletedTime", "Deleted_Time")]
    [InlineData("DeletedId", "Deleted_Id")]
    [InlineData("DeletedBy", "Deleted_By")]
    public void DeletionColumns_ShouldRemainUpdatable(string propertyName, string expectedColumnName)
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppDeletionEntity), propertyName);

        Assert.Equal(expectedColumnName, column.ColumnName, StringComparer.Ordinal);
        Assert.True(column.IsNullable);
        Assert.False(column.IsOnlyIgnoreUpdate);
    }

    /// <summary>
    /// 完整审计实体的删除三列同样必须保持可更新。
    /// </summary>
    /// <param name="propertyName">删除审计属性名。</param>
    /// <param name="expectedColumnName">期望的列名。</param>
    [Theory]
    [InlineData("DeletedTime", "Deleted_Time")]
    [InlineData("DeletedId", "Deleted_Id")]
    [InlineData("DeletedBy", "Deleted_By")]
    public void FullAuditedDeletionColumns_ShouldRemainUpdatable(string propertyName, string expectedColumnName)
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppFullAuditedEntity), propertyName);

        Assert.Equal(expectedColumnName, column.ColumnName, StringComparer.Ordinal);
        Assert.True(column.IsNullable);
        Assert.False(column.IsOnlyIgnoreUpdate);
    }

    /// <summary>
    /// 聚合根家族的主键与审计列目前**都没有**指定列名 —— 锁定这一实际形状。
    /// </summary>
    /// <remarks>
    /// 【缺陷锚点】框架 <c>SugarAggregateRoot</c> 只给 RowVersion 写了 ColumnName，其余列一律留空，
    /// 而实体家族全部显式写了 snake_case。CodeFirst 因此为聚合根表建出 PascalCase 列
    /// （PostgreSQL 未加引号标识符再折叠为小写：basicid / createdtime / isdeleted），
    /// 与实体家族表的 basic_id / created_time / is_deleted 形成两套命名并存。
    /// <para>
    /// 本断言不表态"这样是对的"，它的作用是：谁要"顺手统一"列名，测试立刻变红，
    /// 提醒这属于线上列改名，必须配套写重命名升级脚本。改动前请连同本注释一起评估。
    /// </para>
    /// </remarks>
    /// <param name="propertyName">聚合根上的属性名。</param>
    [Theory]
    [InlineData("BasicId")]
    [InlineData("CreatedTime")]
    [InlineData("CreatedId")]
    [InlineData("CreatedBy")]
    [InlineData("ModifiedTime")]
    [InlineData("ModifiedId")]
    [InlineData("ModifiedBy")]
    [InlineData("IsDeleted")]
    [InlineData("DeletedTime")]
    [InlineData("DeletedId")]
    [InlineData("DeletedBy")]
    public void AggregateRootColumns_StillHaveNoExplicitColumnName(string propertyName)
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppAggregateRoot), propertyName);

        Assert.True(
            string.IsNullOrEmpty(column.ColumnName),
            $"聚合根的 {propertyName} 补了 ColumnName：这是线上列改名，必须配套升级脚本后再更新本断言。");
    }

    /// <summary>
    /// 聚合根的审计列虽然没写列名，但更新语义标志必须与实体家族一致。
    /// </summary>
    /// <remarks>
    /// 命名可以两套，更新语义不能两套：创建列忽略更新、修改与删除列参与更新，
    /// 这一条在聚合根表（SysUser / SysTenant / SysRole 等核心表）上同样是硬要求。
    /// </remarks>
    /// <param name="propertyName">聚合根上的属性名。</param>
    /// <param name="expectedIgnoreOnUpdate">该列是否应当在 UPDATE 中被忽略。</param>
    [Theory]
    [InlineData("CreatedTime", true)]
    [InlineData("CreatedId", true)]
    [InlineData("CreatedBy", true)]
    [InlineData("ModifiedTime", false)]
    [InlineData("ModifiedId", false)]
    [InlineData("ModifiedBy", false)]
    [InlineData("IsDeleted", false)]
    [InlineData("DeletedTime", false)]
    [InlineData("DeletedId", false)]
    [InlineData("DeletedBy", false)]
    public void AggregateRootAuditColumns_ShouldKeepSameUpdateSemantics(string propertyName, bool expectedIgnoreOnUpdate)
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppAggregateRoot), propertyName);

        Assert.Equal(expectedIgnoreOnUpdate, column.IsOnlyIgnoreUpdate);
    }

    /// <summary>
    /// 聚合根的 IsDeleted 与 CreatedTime 必须非空，其余审计列可空。
    /// </summary>
    /// <param name="propertyName">聚合根上的属性名。</param>
    /// <param name="expectedNullable">该列是否允许 NULL。</param>
    [Theory]
    [InlineData("CreatedTime", false)]
    [InlineData("IsDeleted", false)]
    [InlineData("CreatedId", true)]
    [InlineData("CreatedBy", true)]
    [InlineData("ModifiedTime", true)]
    [InlineData("DeletedTime", true)]
    public void AggregateRootAuditColumns_ShouldKeepSameNullability(string propertyName, bool expectedNullable)
    {
        var column = CoreTestHelper.RequireSugarColumn(typeof(BasicAppAggregateRoot), propertyName);

        Assert.Equal(expectedNullable, column.IsNullable);
    }
}
