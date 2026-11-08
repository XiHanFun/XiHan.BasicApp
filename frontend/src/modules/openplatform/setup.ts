/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { registerModuleLocales } from '~/locales'

/**
 * 登记本包文案
 * 个人中心的「开放接口」「OAuth 应用」页签在壳层，经 AppContext 接 my-api-credentials.* 与 my-oauth-apps.*
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
