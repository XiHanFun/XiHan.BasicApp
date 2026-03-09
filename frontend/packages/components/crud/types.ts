import type { SelectOption } from '~/types'

export type CrudFieldType = 'input' | 'select' | 'multi-select'

export interface CrudSearchField {
  key: string
  label?: string
  type?: CrudFieldType
  placeholder?: string
  width?: number | string
  options?: SelectOption[]
  props?: Record<string, any>
}
