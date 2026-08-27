// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Reflection;
using XiHan.BasicApp.Printing.Application.Dtos;
using XiHan.BasicApp.Printing.Application.Mappers;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模板 DTO、领域命令与实体之间的映射契约测试。
/// </summary>
/// <remarks>
/// 行版本对外是十进制**字符串**：数据库里是 long，超过 2^53 后 JavaScript 的 number 会丢精度，
/// 客户端一旦把它读成近似值再回传，乐观并发就会误判成"没被改过"而覆盖别人的设计。
/// 因此出站必须走 InvariantCulture 定死格式、入站必须严格解析（不接受符号、空白、小数与千分位）。
/// </remarks>
public sealed class PrintingExtraMapperContractTests
{
    private const string TemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 创建 DTO 的每个字段都要原样进入领域命令，映射层不做任何默认值补齐或改写。
    /// </summary>
    [Fact]
    public void ToCreateCommand_ShouldCopyEveryField()
    {
        var command = PrintTemplateApplicationMapper.ToCreateCommand(new PrintTemplateCreateDto
        {
            TemplateCode = "SHIP",
            DataSourceCode = "system.print-demo",
            TemplateName = "发货单",
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.61",
            AllowTenantUse = true,
            Status = EnableStatus.Disabled,
            Sort = 42,
            Remark = "备注"
        });

        Assert.Equal("SHIP", command.TemplateCode);
        Assert.Equal("system.print-demo", command.DataSourceCode);
        Assert.Equal("发货单", command.TemplateName);
        Assert.Equal(TemplateJson, command.TemplateJson);
        Assert.Equal("0.0.61", command.EngineVersion);
        Assert.True(command.AllowTenantUse);
        Assert.Equal(EnableStatus.Disabled, command.Status);
        Assert.Equal(42, command.Sort);
        Assert.Equal("备注", command.Remark);
    }

    /// <summary>
    /// 更新 DTO 的每个字段都要进入更新命令，行版本按十进制解析成 long。
    /// </summary>
    [Fact]
    public void ToUpdateCommand_ShouldCopyEveryFieldAndParseRowVersion()
    {
        var command = PrintTemplateApplicationMapper.ToUpdateCommand(new PrintTemplateUpdateDto
        {
            BasicId = 1001,
            RowVersion = "12",
            DataSourceCode = null,
            TemplateName = "订单模板",
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.60",
            AllowTenantUse = true,
            Sort = 7,
            Remark = null
        });

        Assert.Equal(1001, command.Id);
        Assert.Equal(12, command.ExpectedRowVersion);
        Assert.Null(command.DataSourceCode);
        Assert.Equal("订单模板", command.TemplateName);
        Assert.True(command.AllowTenantUse);
        Assert.Equal(7, command.Sort);
        Assert.Null(command.Remark);
    }

    /// <summary>
    /// 状态与删除 DTO 同样携带行版本，映射后的命令保留主键与目标状态。
    /// </summary>
    [Fact]
    public void ToStatusAndDeleteCommand_ShouldCarryIdAndRowVersion()
    {
        var status = PrintTemplateApplicationMapper.ToStatusCommand(new PrintTemplateStatusUpdateDto
        {
            BasicId = 1001,
            RowVersion = "3",
            Status = EnableStatus.Disabled,
            Remark = "停用"
        });
        var delete = PrintTemplateApplicationMapper.ToDeleteCommand(new PrintTemplateDeleteDto
        {
            BasicId = 1002,
            RowVersion = "0"
        });

        Assert.Equal(1001, status.Id);
        Assert.Equal(3, status.ExpectedRowVersion);
        Assert.Equal(EnableStatus.Disabled, status.Status);
        Assert.Equal("停用", status.Remark);
        Assert.Equal(1002, delete.Id);
        Assert.Equal(0, delete.ExpectedRowVersion);
    }

    /// <summary>
    /// 合法行版本字符串必须原样解析，前导零与 long 上界都要支持。
    /// </summary>
    /// <param name="value">行版本字符串。</param>
    /// <param name="expected">期望解析结果。</param>
    [Theory]
    [InlineData("0", 0L)]
    [InlineData("1", 1L)]
    [InlineData("007", 7L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    public void ParseRowVersion_ValidInput_ShouldParse(string value, long expected)
    {
        Assert.Equal(expected, PrintTemplateApplicationMapper.ParseRowVersion(value));
    }

    /// <summary>
    /// 任何带符号、空白、小数点、千分位或非数字的输入都必须拒绝；long 溢出同样拒绝。
    /// </summary>
    /// <param name="value">非法行版本字符串。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("1.0")]
    [InlineData("1,000")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("abc")]
    [InlineData("0x10")]
    [InlineData("1e3")]
    [InlineData("9223372036854775808")]
    public void ParseRowVersion_InvalidInput_ShouldRejectWithFriendlyMessage(string? value)
    {
        var exception = Assert.Throws<UserFriendlyException>(() => PrintTemplateApplicationMapper.ParseRowVersion(value!));

        Assert.Contains("行版本无效", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 实体到详情 DTO 的往返必须不丢字段：列表项字段、模板正文与两个审计时间都要带出。
    /// </summary>
    [Fact]
    public void ToDetailDto_ShouldCarryEveryExposedField()
    {
        var template = CreateTemplate(tenantId: 7);
        template.Remark = "订单打印";
        template.ModifiedTime = DateTimeOffset.UnixEpoch.AddDays(2);

        var detail = PrintTemplateApplicationMapper.ToDetailDto(template);

        Assert.Equal(template.BasicId, detail.BasicId);
        Assert.Equal(template.TemplateCode, detail.TemplateCode);
        Assert.Equal(template.DataSourceCode, detail.DataSourceCode);
        Assert.Equal(template.TemplateName, detail.TemplateName);
        Assert.Equal(template.TemplateJson, detail.TemplateJson);
        Assert.Equal(template.EngineVersion, detail.EngineVersion);
        Assert.Equal(template.AllowTenantUse, detail.AllowTenantUse);
        Assert.Equal(template.Status, detail.Status);
        Assert.Equal(template.Sort, detail.Sort);
        Assert.Equal(template.Remark, detail.Remark);
        Assert.Equal(template.CreatedTime, detail.CreatedTime);
        Assert.Equal(template.ModifiedTime, detail.ModifiedTime);
        Assert.False(detail.IsGlobal);
    }

    /// <summary>
    /// 详情 DTO 继承列表项 DTO，两者在共有属性上必须给出完全一致的值。
    /// </summary>
    [Fact]
    public void ToDetailDto_ShouldAgreeWithListItemOnSharedProperties()
    {
        var template = CreateTemplate(tenantId: 0);
        var item = PrintTemplateApplicationMapper.ToListItemDto(template);
        var detail = PrintTemplateApplicationMapper.ToDetailDto(template);

        var mismatched = typeof(PrintTemplateListItemDto)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => !Equals(property.GetValue(item), property.GetValue(detail)))
            .Select(property => property.Name)
            .ToList();

        Assert.True(mismatched.Count == 0, $"详情与列表项在共有属性上取值不一致：{string.Join("、", mismatched)}");
    }

    /// <summary>
    /// 全局模板的 IsGlobal 由租户号派生，映射后必须为 true 且保留开放标记。
    /// </summary>
    [Fact]
    public void ToListItemDto_GlobalTemplate_ShouldReportIsGlobal()
    {
        var template = CreateTemplate(tenantId: 0);
        template.AllowTenantUse = true;

        var item = PrintTemplateApplicationMapper.ToListItemDto(template);

        Assert.True(item.IsGlobal);
        Assert.True(item.AllowTenantUse);
    }

    /// <summary>
    /// 行版本出站必须是不变文化的纯十进制串，long 上界不能被科学计数法或分组符号改写。
    /// </summary>
    /// <param name="rowVersion">实体行版本。</param>
    /// <param name="expected">期望输出字符串。</param>
    [Theory]
    [InlineData(0L, "0")]
    [InlineData(4L, "4")]
    [InlineData(9007199254740993L, "9007199254740993")]
    [InlineData(long.MaxValue, "9223372036854775807")]
    public void ToListItemDto_RowVersion_ShouldUseInvariantDecimalText(long rowVersion, string expected)
    {
        var template = CreateTemplate(tenantId: 7);
        template.RowVersion = rowVersion;

        var item = PrintTemplateApplicationMapper.ToListItemDto(template);

        Assert.Equal(expected, item.RowVersion);
        Assert.Equal(rowVersion, PrintTemplateApplicationMapper.ParseRowVersion(item.RowVersion));
    }

    /// <summary>
    /// 出站行版本与入站解析必须构成往返闭环，不因当前线程文化而漂移。
    /// </summary>
    [Fact]
    public void RowVersion_ShouldRoundTripUnderNonInvariantCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var template = CreateTemplate(tenantId: 7);
            template.RowVersion = 1234567L;

            var item = PrintTemplateApplicationMapper.ToListItemDto(template);

            Assert.Equal("1234567", item.RowVersion);
            Assert.Equal(1234567L, PrintTemplateApplicationMapper.ParseRowVersion(item.RowVersion));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// 自由模板的空数据源必须原样保持 null，不得被映射成空字符串。
    /// </summary>
    [Fact]
    public void ToDetailDto_FreeTemplate_ShouldKeepNullDataSource()
    {
        var template = CreateTemplate(tenantId: 7);
        template.DataSourceCode = null;

        Assert.Null(PrintTemplateApplicationMapper.ToDetailDto(template).DataSourceCode);
    }

    /// <summary>
    /// 六个映射入口都必须以空入参的 <see cref="ArgumentNullException"/> 开场。
    /// </summary>
    [Fact]
    public void Mappers_NullInput_ShouldThrowArgumentNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() => PrintTemplateApplicationMapper.ToCreateCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => PrintTemplateApplicationMapper.ToUpdateCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => PrintTemplateApplicationMapper.ToStatusCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => PrintTemplateApplicationMapper.ToDeleteCommand(null!));
        _ = Assert.Throws<ArgumentNullException>(() => PrintTemplateApplicationMapper.ToListItemDto(null!));
        _ = Assert.Throws<ArgumentNullException>(() => PrintTemplateApplicationMapper.ToDetailDto(null!));
    }

    /// <summary>
    /// 列表项 DTO 不得暴露模板正文，列表接口一次返回上百条设计 JSON 会直接压垮响应体。
    /// </summary>
    [Fact]
    public void ListItemDto_ShouldNotExposeTemplateJson()
    {
        Assert.Null(typeof(PrintTemplateListItemDto).GetProperty("TemplateJson"));
        Assert.NotNull(typeof(PrintTemplateDetailDto).GetProperty("TemplateJson"));
    }

    /// <summary>
    /// 创建带稳定主键与审计时间的模板实体。
    /// </summary>
    private static SysPrintTemplate CreateTemplate(long tenantId)
    {
        var template = new SysPrintTemplate
        {
            TenantId = tenantId,
            TemplateCode = "ORDER",
            DataSourceCode = "system.print-demo",
            TemplateName = "订单模板",
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.60",
            Status = EnableStatus.Enabled,
            Sort = 10,
            RowVersion = 4,
            CreatedTime = DateTimeOffset.UnixEpoch
        };
        typeof(SysPrintTemplate)
            .GetProperty(nameof(SysPrintTemplate.BasicId), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(template, 1001L);
        return template;
    }
}
