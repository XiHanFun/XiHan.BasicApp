/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { registerWelcomeHighlights, useAppContext, useAppStore, useLayoutBridgeStore } from '~/stores'
import { passkeysSupported } from '~/utils'

/** 壳层这几屏介绍的版本线 */
const RELEASE = '6.0'

let registered = false

/**
 * 登记壳层自己的亮点：界面、命令面板、灵动岛、通行密钥
 * 各能力包的亮点由它们的 setup 钩子登记（聊天、工作台），排序号与这里穿插
 */
export function registerShellWelcomeHighlights(): void {
  if (registered)
    return
  registered = true
  const profile = useAppContext().shellRoutes.profile

  registerWelcomeHighlights(
    {
      release: RELEASE,
      key: 'shell',
      order: 10,
      icon: 'lucide:panel-left',
      tone: 'brand',
      categoryKey: 'welcome.shell.category',
      titleKey: 'welcome.shell.title',
      descriptionKey: 'welcome.shell.description',
      pointKeys: ['welcome.shell.point_layout', 'welcome.shell.point_theme', 'welcome.shell.point_sync'],
      demo: () => import('./demos/ShellDemo.vue'),
      action: {
        labelKey: 'welcome.shell.action',
        run: () => useLayoutBridgeStore().requestOpenPreferenceDrawer(),
      },
    },
    {
      release: RELEASE,
      key: 'command',
      order: 20,
      icon: 'lucide:command',
      tone: 'info',
      categoryKey: 'welcome.command.category',
      titleKey: 'welcome.command.title',
      descriptionKey: 'welcome.command.description',
      pointKeys: ['welcome.command.point_palette', 'welcome.command.point_tabs', 'welcome.command.point_split'],
      demo: () => import('./demos/CommandDemo.vue'),
      // 偏好里把搜索入口关掉的人看不到命令面板，这一屏随之不出现
      when: () => useAppStore().widgetSearchPlacement !== 'hidden',
      action: {
        labelKey: 'welcome.command.action',
        run: () => useLayoutBridgeStore().requestOpenGlobalSearch(),
      },
    },
    {
      release: RELEASE,
      key: 'island',
      order: 30,
      icon: 'lucide:rectangle-horizontal',
      tone: 'success',
      categoryKey: 'welcome.island.category',
      titleKey: 'welcome.island.title',
      descriptionKey: 'welcome.island.description',
      pointKeys: ['welcome.island.point_export', 'welcome.island.point_sync', 'welcome.island.point_call'],
      demo: () => import('./demos/IslandDemo.vue'),
      when: () => useAppStore().widgetDynamicIsland,
    },
    {
      release: RELEASE,
      key: 'passkey',
      order: 50,
      icon: 'lucide:key-round',
      tone: 'success',
      categoryKey: 'welcome.passkey.category',
      titleKey: 'welcome.passkey.title',
      descriptionKey: 'welcome.passkey.description',
      pointKeys: ['welcome.passkey.point_passkey', 'welcome.passkey.point_two_factor', 'welcome.passkey.point_devices'],
      demo: () => import('./demos/PasskeyDemo.vue'),
      // 浏览器不支持、或登录方式里没开放通行密钥时，这一屏不出现
      when: async () => passkeysSupported() && (await useAppContext().apis.getLoginConfigApi()).loginMethods.includes('passkey'),
      // 通行密钥在个人中心里添加；应用没有登记个人中心的路由时不给入口
      action: profile
        ? { labelKey: 'welcome.passkey.action', to: profile }
        : undefined,
    },
  )
}
