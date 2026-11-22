/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ListFieldSchema } from './types'
import type { PresetSnapshot } from './usePresetManager'
import { i18n } from '~/locales'
import { formatDate, getOptionLabel } from '~/utils'

/**
 * 条件预设的文字说明：筛选与排序各写成一串「名称：值」，界面逐项列出，悬停提示合成一句
 * 字段名与选项名取页面字段（已翻译、已按权限过滤）；页面字段里没有的筛选键照键名列出，存了什么就说什么
 */
export interface ConditionSummary {
  filters: string[]
  sorts: string[]
  pageSize?: number
}

function t(key: string, args: Record<string, unknown> = {}): string {
  return i18n.global.t(key, args)
}

function pair(label: string, value: string): string {
  return t('component.schema_presets.pair', { label, value })
}

function isBlank(value: unknown): boolean {
  return value === null || value === undefined || value === ''
    || (Array.isArray(value) && value.every(item => item === null || item === undefined || item === ''))
}

function isDateField(field: ListFieldSchema): boolean {
  return field.dataType === 'date' || field.dataType === 'datetime'
}

/** 按语言连成一串：筛选与排序是「且」，多选的值是「或」 */
function joinList(items: string[], type: 'conjunction' | 'disjunction'): string {
  return new Intl.ListFormat(String(i18n.global.locale.value), { style: 'narrow', type }).format(items)
}

function formatScalar(field: ListFieldSchema, value: unknown): string {
  if (isDateField(field))
    return formatDate(value as number | string, 'YYYY-MM-DD')
  if (field.options?.length) {
    const optionValue = typeof value === 'boolean'
      ? String(value)
      : value as number | string
    return getOptionLabel(field.options as Array<{ label: string, value: number | string }>, optionValue, String(value))
  }
  if (typeof value === 'boolean') {
    return value
      ? t('common.statuses.yes')
      : t('common.statuses.no')
  }
  return String(value)
}

function formatValue(field: ListFieldSchema | undefined, value: unknown): string {
  if (!Array.isArray(value)) {
    return field
      ? formatScalar(field, value)
      : String(value)
  }
  const items = value.filter(item => !isBlank(item))
  if (field?.searchRange && isDateField(field))
    return items.map(item => formatScalar(field, item)).join(' ~ ')
  return joinList(items.map(item => (field
    ? formatScalar(field, item)
    : String(item))), 'disjunction')
}

/** 把一份筛选、排序与每页条数说成人看得懂的几项 */
export function describeConditions(fields: readonly ListFieldSchema[], snapshot: PresetSnapshot): ConditionSummary {
  const byKey = new Map(fields.map(field => [field.key, field]))
  const filters = Object.entries(snapshot.filters)
    .filter(([, value]) => !isBlank(value))
    .map(([key, value]) => {
      const field = byKey.get(key)
      return pair(field?.title ?? key, formatValue(field, value))
    })
  const sorts = (snapshot.sorts ?? []).map(rule => pair(
    byKey.get(rule.field)?.title ?? rule.field,
    rule.order === 'desc'
      ? t('component.schema_presets.desc')
      : t('component.schema_presets.asc'),
  ))
  return {
    filters,
    sorts,
    pageSize: snapshot.pageSize,
  }
}

/** 一组条件连成一串，没有就写「无」 */
export function conditionText(items: string[]): string {
  return items.length
    ? joinList(items, 'conjunction')
    : t('component.schema_presets.none')
}

/** 悬停提示用的一句话：筛选与排序 */
export function conditionSentence(summary: ConditionSummary): string {
  return t('component.schema_presets.summary', {
    filters: conditionText(summary.filters),
    sorts: conditionText(summary.sorts),
  })
}
