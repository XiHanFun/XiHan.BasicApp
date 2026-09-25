export type LogDetailFieldType = 'boolean' | 'bytes' | 'code' | 'date' | 'duration' | 'enum' | 'text'

export interface LogDetailOption {
  label: string
  value: number | string
}

export interface LogDetailField {
  /** 后端枚举类型名：标签取后端枚举元数据，前端不另抄一份，后端加了成员也照样显示；与 options 二选一 */
  enumName?: string
  falseText?: string
  key: string
  label: string
  options?: LogDetailOption[]
  span?: 1 | 2
  trueText?: string
  type?: LogDetailFieldType
}
