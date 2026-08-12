// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Domain.DataSources;

/// <summary>
/// 打印模块内置数据源定义。
/// </summary>
public static class BuiltInPrintDataSources
{
    /// <summary>
    /// 系统打印示例（system.print-demo）：演示字段全类型与明细表的样板数据源。
    /// </summary>
    public static PrintDataSourceDefinition SystemPrintDemo { get; } = new(
        "system.print-demo",
        "系统打印示例",
        [
            new("title", "标题"),
            new("documentNo", "单据编号"),
            new("customerName", "客户名称"),
            new("createdTime", "制单时间", InputType: "datetime"),
            new("logo", "Logo", "image"),
            new("barcode", "单据条码", "barcode"),
            new("qrCode", "单据二维码", "qrcode"),
            new("items", "明细表", "table",
            [
                new("sku", "物料编码", 90),
                new("name", "物料名称", 120),
                new("quantity", "数量", 60, "number"),
                new("unit", "单位", 50)
            ])
        ],
        """
        {
          "title": "XiHan BasicApp 打印示例",
          "documentNo": "DEMO-20260729-0001",
          "customerName": "示例客户",
          "createdTime": "2026-07-29 10:00:00",
          "logo": "/favicon.png",
          "barcode": "DEMO-20260729-0001",
          "qrCode": "https://basicapp.xihanfun.com/print-demo/DEMO-20260729-0001",
          "items": [
            { "sku": "SKU-001", "name": "示例物料 A", "quantity": 2, "unit": "件" },
            { "sku": "SKU-002", "name": "示例物料 B", "quantity": 5, "unit": "箱" }
          ]
        }
        """);
}
