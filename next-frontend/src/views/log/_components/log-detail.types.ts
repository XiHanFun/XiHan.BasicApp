export type LogDetailFieldType = 'boolean' | 'bytes' | 'code' | 'date' | 'duration' | 'enum' | 'text'

export interface LogDetailOption {
  label: string
  value: number | string
}

export interface LogDetailField {
  falseText?: string
  key: string
  label: string
  options?: LogDetailOption[]
  span?: 1 | 2
  trueText?: string
  type?: LogDetailFieldType
}
