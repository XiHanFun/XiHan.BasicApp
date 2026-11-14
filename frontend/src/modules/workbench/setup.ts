/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { registerModuleLocales } from '~/locales'
import { registerWelcomeHighlights } from '~/stores'

/**
 * 登记本模块文案与新版欢迎页里介绍工作台的一屏
 * 页面由后端菜单下发（BasicApp:Modules:Workbench:Enabled 关掉即不下发，欢迎页那一屏随之不出现）
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
  registerWelcomeHighlights({
    release: '6.0',
    key: 'workbench',
    order: 60,
    icon: 'lucide:layout-dashboard',
    tone: 'warning',
    categoryKey: 'workbench.welcome.category',
    titleKey: 'workbench.welcome.title',
    descriptionKey: 'workbench.welcome.description',
    pointKeys: ['workbench.welcome.point_widgets', 'workbench.welcome.point_arrange', 'workbench.welcome.point_favorites'],
    demo: () => import('./components/WelcomeDemo.vue'),
    page: 'workbench.dashboard',
    action: { labelKey: 'workbench.welcome.action', to: '/workbench/dashboard' },
  })
}
