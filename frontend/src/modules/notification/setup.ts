/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { registerModuleLocales } from '~/locales'

/**
 * 登记本包文案
 * 顶栏通知中心与横幅、弹窗、强制阅读在壳层，经 AppContext 接 inbox.*
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
