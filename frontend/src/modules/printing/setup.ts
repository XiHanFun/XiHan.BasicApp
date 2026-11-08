/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { RemotePrintDataSourceDto, ResolvedPrintTemplate } from '~/printing'
import { printDataSources, printTemplates } from '~/contracts'
import { registerModuleLocales } from '~/locales'
import { configurePrinting } from '~/printing'

/**
 * 登记本模块文案，把后端的模板解析与数据源目录接进公共打印包
 * 数据源目录以后端注册表为准，惰性拉取；打印运行时与设计器首次用到时才加载
 * 页面由后端菜单下发（BasicApp:Modules:Printing:Enabled 关掉即不下发），登记无副作用
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
  configurePrinting({
    resolveTemplate: async (templateCode, scope): Promise<ResolvedPrintTemplate> => {
      const resolved = await printTemplates.resolve({ templateCode, scope })
      // 打印包的字段名：主键叫 basicId，并发戳叫 rowVersion（样例表单据它判断模板有没有变）
      return {
        basicId: resolved.id,
        rowVersion: resolved.concurrencyStamp,
        templateCode: resolved.templateCode,
        templateName: resolved.templateName,
        templateJson: resolved.templateJson,
        dataSourceCode: resolved.dataSourceCode ?? null,
        requestedScope: resolved.requestedScope,
        resolvedScope: resolved.resolvedScope,
      }
    },
    // 字段种类在契约里是字符串：后端启动时已校验只有 text / image / barcode / qrcode / table，打印包登记时再校验一遍
    listDataSources: async () => (await printDataSources.list({})).items as RemotePrintDataSourceDto[],
  })
}
