/**
 * SchemaPage 的本地 CSV：单元格按字段类型与选项写文本，不走列 render
 */
import type { ListFieldSchema } from './types'
import { describe, expect, it } from 'vitest'
import { formatFieldText } from './renderer'
import { toCsv } from './useSchemaExport'

const pausedField: ListFieldSchema = {
  key: 'paused',
  title: '状态',
  dataType: 'enum',
  options: [
    { label: '启用', value: 'false' },
    { label: '禁用', value: 'true' },
  ],
}

describe('本地 CSV 的单元格', () => {
  it('布尔列的选项值是筛选用的字符串 true / false，按行里的布尔值匹配到文案', () => {
    expect(formatFieldText(pausedField, { paused: true })).toBe('禁用')
    expect(formatFieldText(pausedField, { paused: false })).toBe('启用')
  })

  it('字符串枚举照常匹配，匹配不到的显示占位', () => {
    const field: ListFieldSchema = {
      key: 'kind',
      title: '类型',
      dataType: 'enum',
      options: [
        { label: '定时', value: 'Cron' },
      ],
    }
    expect(formatFieldText(field, { kind: 'Cron' })).toBe('定时')
    expect(formatFieldText(field, { kind: 'Other' })).toBe('-')
    expect(formatFieldText(field, { kind: null })).toBe('-')
  })

  it('表头取字段标题，带 BOM，含逗号的单元格加引号', () => {
    const name: ListFieldSchema = { key: 'name', title: '名称', dataType: 'string' }
    expect(toCsv([name, pausedField], [
      { name: 'a,b', paused: true },
    ])).toBe('﻿名称,状态\r\n"a,b",禁用')
  })
})
