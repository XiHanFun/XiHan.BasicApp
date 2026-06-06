import type { VNodeChild } from 'vue'
import type { ListFieldSchema } from './types'
import { NTag } from 'naive-ui'
import { h } from 'vue'
import { formatDate, getOptionLabel } from '~/utils'

/** 安全读取行字段值（兼容具名 DTO 接口，无索引签名） */
function readField(row: object, key: string): unknown {
  return (row as Record<string, unknown>)[key]
}

/** options 转可变数组（getOptionLabel 形参要求可变） */
function toMutableOptions(
  options: ReadonlyArray<{ label: string, value: string | number }>,
): Array<{ label: string, value: string | number }> {
  return options as Array<{ label: string, value: string | number }>
}

/**
 * 字段脱敏：内置常用 formatter，供字段权限脱敏使用。
 * S2 可由后端 FLS 规则下发，覆盖此处默认。
 */
const maskers: Record<string, (value: string) => string> = {
  maskPhone: value => (value.length >= 11 ? `${value.slice(0, 3)}****${value.slice(-4)}` : value),
  maskEmail: (value) => {
    const at = value.indexOf('@')
    if (at <= 1) {
      return value
    }
    return `${value[0]}***${value.slice(at)}`
  },
  maskIdCard: value => (value.length >= 15 ? `${value.slice(0, 3)}************${value.slice(-4)}` : value),
}

/**
 * 解析格式化文本值（不含富渲染，供导出/详情复用）。
 */
export function formatFieldText<TRow extends object>(
  field: ListFieldSchema<TRow>,
  row: TRow,
): string {
  const raw = readField(row, field.key)

  const masker = field.formatter ? maskers[field.formatter] : undefined
  if (masker && raw != null) {
    return masker(String(raw))
  }

  if (raw == null || raw === '') {
    return '-'
  }

  switch (field.dataType) {
    case 'date':
      return formatDate(String(raw), 'YYYY-MM-DD')
    case 'datetime':
      return formatDate(String(raw))
    case 'boolean':
      return raw ? '是' : '否'
    case 'enum':
    case 'tag':
      return field.options
        ? getOptionLabel(toMutableOptions(field.options), raw as string | number)
        : String(raw)
    case 'percent':
      return `${raw}%`
    case 'money':
      return `¥ ${Number(raw).toLocaleString()}`
    default:
      return String(raw)
  }
}

/**
 * 渲染单元格 VNode（表格列 render 使用）。
 * 自定义 field.render 优先级最高；其次按 dataType 走内置渲染；兜底为格式化文本。
 */
export function renderFieldCell<TRow extends object>(
  field: ListFieldSchema<TRow>,
  row: TRow,
): VNodeChild {
  if (field.render) {
    return field.render(row)
  }

  const raw = readField(row, field.key)

  // 标签型：用字典 options 映射 label，并以 NTag 着色
  if ((field.dataType === 'tag' || field.dataType === 'enum') && field.options) {
    if (raw == null) {
      return '-'
    }
    const label = getOptionLabel(toMutableOptions(field.options), raw as string | number)
    return h(NTag, { bordered: false, round: true, size: 'small' }, () => label)
  }

  if (field.dataType === 'boolean') {
    if (raw == null) {
      return '-'
    }
    return h(
      NTag,
      { bordered: false, round: true, size: 'small', type: raw ? 'success' : 'default' },
      () => (raw ? '是' : '否'),
    )
  }

  return formatFieldText(field, row)
}
