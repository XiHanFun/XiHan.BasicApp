import type { TagProps } from 'naive-ui'

/** naive-ui 标签语气 */
export type SchemaTagType = NonNullable<TagProps['type']>

/**
 * 状态类枚举的标签配色表：字典码 → 枚举值 → 标签语气。
 *
 * 枚举值按后端 JsonStringEnumConverter 的序列化字符串书写：packages 层不可反向依赖
 * src 下的枚举定义，故此处以字面量对齐。
 */
const STATUS_TAG_TYPES: Readonly<Record<string, Readonly<Record<string, SchemaTagType>>>> = {
  EnableStatus: {
    Enabled: 'success',
    Disabled: 'error',
  },
  TenantConfigStatus: {
    Configured: 'success',
    Configuring: 'info',
    Pending: 'warning',
    Failed: 'error',
    Disabled: 'default',
  },
}

/**
 * 解析状态标签语气
 *
 * @param dictionaryCode 字段的字典码（即后端枚举类型名）
 * @param value 该行的枚举值
 * @returns 已登记的语气；字典码或枚举值未登记时返回 undefined，由调用方按中性标签渲染
 */
export function resolveStatusTagType(dictionaryCode: string | undefined, value: unknown): SchemaTagType | undefined {
  if (!dictionaryCode || typeof value !== 'string') {
    return undefined
  }
  return STATUS_TAG_TYPES[dictionaryCode]?.[value]
}
