/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { GuideChapter, GuideStep, GuideTarget } from '~/stores'
import { useIsMobile } from '~/composables/useIsMobile'
import { i18n } from '~/locales'
import { useAppStore } from '~/stores'

/**
 * 列表页的页面引导：通用的一章介绍搜索区、操作区与表格，业务页在 PageSchema.guide 里另写自己那一章
 * 锚点（data-guide）打在 SchemaPage 及其面板上；导入、导出、高级搜索这类按页面与权限出现的，开播前自动去掉
 */

/** 通用章节的内容版本：步骤有实质变化时加一，看过旧版的人会再看到一次 */
const SCHEMA_GUIDE_VERSION = 1

/** 业务页自己那一章 */
export interface SchemaGuide {
  /** 内容版本：步骤有实质变化时加一 */
  version: number
  steps: readonly GuideStep[]
}

/** 业务步骤取列表页里元素的写法：锚点都在页面根里找，不跨到别的标签页 */
export const schemaGuideTarget = {
  /**
   * 某一列的列头
   * @param key 字段键
   */
  column: (key: string): GuideTarget => root => root.querySelector(`[data-guide-head="${key}"]`),
  /**
   * 第一行里某一列的单元格
   * @param key 字段键
   */
  cell: (key: string): GuideTarget => root => root.querySelector(`[data-guide-cell="${key}"]`),
  /**
   * 页面级操作按钮
   * @param key 操作键（PageSchema.actions 里 scope 为 page 的那一项）
   */
  action: (key: string): GuideTarget => root => root.querySelector(`[data-guide-action="${key}"]`),
  /** 第一行的行操作（「更多」菜单的触发钮） */
  rowActions: (): GuideTarget => root => root.querySelector('[data-guide-cell="__actions__"] button'),
}

/** 步骤 id → 目标与气泡方位；标题与说明取 guide.schema 下与 id 同名的那一组文案 */
const SCHEMA_STEPS: ReadonlyArray<Omit<GuideStep, 'titleKey' | 'descriptionKey'>> = [
  { id: 'search', target: 'schema.search', placement: 'bottom-start' },
  { id: 'search_submit', target: 'schema.search-submit', placement: 'bottom-end' },
  { id: 'search_reset', target: 'schema.search-reset', placement: 'bottom-end' },
  { id: 'search_settings', target: 'schema.search-settings', placement: 'bottom-end' },
  { id: 'presets', target: 'schema.presets', placement: 'bottom-end' },
  { id: 'advanced', target: 'schema.advanced', placement: 'bottom-end' },
  { id: 'toolbar', target: 'schema.toolbar', placement: 'bottom-start' },
  { id: 'refresh', target: 'schema.refresh', placement: 'bottom-end' },
  { id: 'import', target: 'schema.import', placement: 'bottom-end' },
  { id: 'export', target: 'schema.export', placement: 'bottom-end' },
  { id: 'table_settings', target: 'schema.table-settings', placement: 'bottom-end' },
  { id: 'fullscreen', target: 'schema.fullscreen', placement: 'bottom-end' },
  { id: 'table', target: 'schema.table', placement: 'top' },
  { id: 'sort', target: 'schema.sort', placement: 'bottom' },
  { id: 'select', target: 'schema.select', placement: 'bottom-start' },
  // 速览随偏好开关，窄屏没有悬停也不出
  {
    id: 'peek',
    target: 'schema.first-row',
    placement: 'bottom',
    when: () => useAppStore().tableRowPeek && !useIsMobile().isMobile.value,
  },
  {
    id: 'row_actions',
    target: schemaGuideTarget.rowActions(),
    placement: 'left',
  },
  { id: 'footer', target: 'schema.footer', placement: 'top' },
]

/** 列表页通用的一章 */
export const schemaGuideChapter: GuideChapter = {
  key: 'schema',
  version: SCHEMA_GUIDE_VERSION,
  title: () => i18n.global.t('guide.schema.title'),
  steps: () => SCHEMA_STEPS.map(step => ({
    ...step,
    titleKey: `guide.schema.${step.id}.title`,
    descriptionKey: `guide.schema.${step.id}.description`,
  })),
}

/**
 * 业务页那一章
 * @param pageCode 页面码，记「看过」用 page:<页面码>
 * @param title 章节名（页面名称）
 * @param guide 业务步骤
 */
export function schemaPageGuideChapter(pageCode: string, title: () => string, guide: SchemaGuide): GuideChapter {
  return {
    key: `page:${pageCode}`,
    version: guide.version,
    title,
    steps: () => guide.steps,
  }
}
