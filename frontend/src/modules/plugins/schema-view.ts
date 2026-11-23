/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { SchemaFieldDataType, SchemaQueryParams } from '~/components'
import type { PageResult } from '~/types/contracts'
import { toSchemaPageResult } from '~/contracts'

/** 声明式页面一列的值种类 */
export type PluginColumnType = 'text' | 'number' | 'boolean' | 'datetime'

/** 声明式页面一个字段的输入控件 */
export type PluginFieldType = 'text' | 'textarea' | 'number' | 'switch'

export interface PluginSchemaColumn {
  key: string
  titleKey: string
  type?: PluginColumnType | null
}

export interface PluginSchemaField {
  key: string
  labelKey: string
  helpKey?: string | null
  type?: PluginFieldType | null
  required?: boolean
}

/**
 * 声明式页面的描述（插件包里的 `pages/<页>.json`，安装时后端已按同一规则核对）
 * 列表、新建、修改、删除各绑插件的一个操作；按钮的权限取操作要的权限码
 */
export interface PluginSchemaView {
  list: string
  create?: string | null
  update?: string | null
  delete?: string | null
  rowKey?: string | null
  columns: PluginSchemaColumn[]
  form?: PluginSchemaField[] | null
}

export type PluginRow = Record<string, unknown>

export type PluginFormValue = string | number | boolean | null

export function parseSchemaView(json: string): PluginSchemaView {
  return JSON.parse(json) as PluginSchemaView
}

export function rowKeyOf(view: PluginSchemaView): string {
  return view.rowKey || 'id'
}

export function columnDataType(type: PluginColumnType | null | undefined): SchemaFieldDataType {
  switch (type) {
    case 'number': return 'number'
    case 'boolean': return 'boolean'
    case 'datetime': return 'datetime'
    default: return 'string'
  }
}

/**
 * 列表操作一次取回全部行：关键字在各列的显示文本里找，再按页切片
 * 插件的列表操作不分页、不收筛选（方案 6.2），筛选与分页都在浏览器里做
 */
export function pageRows(rows: readonly PluginRow[], view: PluginSchemaView, params: SchemaQueryParams): PageResult<PluginRow> {
  const keyword = String(params.filters.keyword ?? '').trim().toLowerCase()
  const matched = keyword
    ? rows.filter(row => view.columns.some(column => String(row[column.key] ?? '').toLowerCase().includes(keyword)))
    : [...rows]
  const start = (params.page - 1) * params.pageSize
  return toSchemaPageResult({
    items: matched.slice(start, start + params.pageSize),
    total: matched.length,
    page: {},
  }, params)
}

/** 弹窗表单的初值：编辑时取行里的值，新建时按控件给空值 */
export function initialForm(fields: readonly PluginSchemaField[], row?: PluginRow): Record<string, PluginFormValue> {
  const form: Record<string, PluginFormValue> = {}
  for (const field of fields) {
    const value = row?.[field.key]
    switch (field.type) {
      case 'number':
        form[field.key] = typeof value === 'number'
          ? value
          : null
        break
      case 'switch':
        form[field.key] = value === true
        break
      default:
        form[field.key] = typeof value === 'string'
          ? value
          : value === undefined || value === null
            ? ''
            : String(value)
    }
  }
  return form
}

/** 表单交给插件操作的请求：文本去掉首尾空白，选填的空文本交 null；修改时带上行键 */
export function formRequest(fields: readonly PluginSchemaField[], form: Readonly<Record<string, PluginFormValue>>, key?: { name: string, value: unknown }): Record<string, unknown> {
  const request: Record<string, unknown> = key
    ? { [key.name]: key.value }
    : {}
  for (const field of fields) {
    const value = form[field.key] ?? null
    if (typeof value === 'string' && (field.type ?? 'text') !== 'switch') {
      const text = value.trim()
      request[field.key] = text === '' && !field.required
        ? null
        : text
    }
    else {
      request[field.key] = value
    }
  }
  return request
}
