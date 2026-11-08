/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { registerModuleLocales } from '~/locales'

/**
 * 运维包的启动钩子：登记本包文案
 * 升级记录的详情抽屉借用审计包的日志详情，文案随审计包登记
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
