/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { GuideChapter, GuideStep } from '~/stores'
import { i18n } from '~/locales'
import { useGuideStore } from '~/stores'

/**
 * 界面导览：菜单、顶栏、标签栏、内容区与页脚，按从左到右、从上到下逐个介绍
 * 锚点（data-guide）打在壳层各组件上；被偏好藏起、挪进悬浮组或这个布局里没有的，开播前自动去掉
 * 步骤有实质变化时把 version 加一，看过旧版的人会再看到一次
 */
const SHELL_GUIDE_VERSION = 1

/** 步骤 id → 锚点与气泡方位；标题与说明取 guide.shell 下与 id 同名的那一组文案 */
const SHELL_STEPS: ReadonlyArray<Pick<GuideStep, 'id' | 'placement'> & { anchor?: string }> = [
  { id: 'intro' },
  { id: 'sidebar', anchor: 'shell.sidebar', placement: 'right-start' },
  { id: 'top_menu', anchor: 'shell.top-menu', placement: 'bottom' },
  { id: 'sidebar_collapse', anchor: 'shell.sidebar-collapse', placement: 'right-end' },
  { id: 'sidebar_toggle', anchor: 'shell.sidebar-toggle', placement: 'bottom-start' },
  { id: 'favorites', anchor: 'shell.favorites', placement: 'bottom-start' },
  { id: 'history', anchor: 'shell.history', placement: 'bottom-start' },
  { id: 'refresh', anchor: 'shell.refresh', placement: 'bottom-start' },
  { id: 'breadcrumb', anchor: 'shell.breadcrumb', placement: 'bottom-start' },
  { id: 'search', anchor: 'shell.search', placement: 'bottom' },
  { id: 'language', anchor: 'shell.language', placement: 'bottom-end' },
  { id: 'timezone', anchor: 'shell.timezone', placement: 'bottom-end' },
  { id: 'theme', anchor: 'shell.theme', placement: 'bottom-end' },
  { id: 'fullscreen', anchor: 'shell.fullscreen', placement: 'bottom-end' },
  { id: 'preferences', anchor: 'shell.preferences', placement: 'bottom-end' },
  { id: 'notifications', anchor: 'shell.notifications', placement: 'bottom-end' },
  { id: 'context', anchor: 'shell.context', placement: 'bottom-end' },
  { id: 'user', anchor: 'shell.user', placement: 'bottom-end' },
  { id: 'float_toolbar', anchor: 'shell.float-toolbar', placement: 'left' },
  { id: 'tabs', anchor: 'shell.tabs', placement: 'bottom-start' },
  { id: 'tab_more', anchor: 'shell.tab-more', placement: 'bottom-end' },
  { id: 'tab_overview', anchor: 'shell.tab-overview', placement: 'bottom-end' },
  { id: 'tab_reload', anchor: 'shell.tab-reload', placement: 'bottom-end' },
  { id: 'tab_maximize', anchor: 'shell.tab-maximize', placement: 'bottom-end' },
  { id: 'content', anchor: 'shell.content', placement: 'top' },
  { id: 'footer', anchor: 'shell.footer', placement: 'top' },
  { id: 'replay', anchor: 'shell.guide', placement: 'bottom-end' },
]

/** 界面导览这一章 */
export const shellGuideChapter: GuideChapter = {
  key: 'shell',
  version: SHELL_GUIDE_VERSION,
  title: () => i18n.global.t('guide.shell.title'),
  steps: () => SHELL_STEPS.map(step => ({
    id: step.id,
    target: step.anchor,
    titleKey: `guide.shell.${step.id}.title`,
    descriptionKey: `guide.shell.${step.id}.description`,
    placement: step.placement,
  })),
}

/** 布局挂载时登记界面导览 */
export function registerShellGuide(): void {
  useGuideStore().registerShell(shellGuideChapter)
}
