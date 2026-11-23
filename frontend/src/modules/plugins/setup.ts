/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { registerModuleLocales } from '~/locales'
import { registerViewSource } from '~/router'
import { pluginViewSource } from './runtime'

/**
 * 插件模块的启动钩子：登记本模块文案，把插件页面接进路由的视图来源
 * 管理页与插件页面都由后端菜单下发（BasicApp:Modules:Plugins:Enabled 关掉即不下发），登记无副作用
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
  registerViewSource(pluginViewSource)
}
