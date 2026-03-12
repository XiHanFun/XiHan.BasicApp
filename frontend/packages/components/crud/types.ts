import type { SelectOption } from '~/types'

export type TableLayout = 'compact' | 'default' | 'loose'

export interface ColumnSettingItem {
  key: string
  title: string
  visible: boolean
  locked: boolean
  fixed?: 'left' | 'right' | false
}

export interface GlobalSettings {
  showCheckbox: boolean
  showIndex: boolean
  layout: TableLayout
}

export interface ColumnPropertySettings {
  global: GlobalSettings
  columns: ColumnSettingItem[]
}

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

export interface CrudPaginationConfig {
  page: number
  pageSize: number
  total: number
  pageSizes?: number[]
  showQuickJumper?: boolean
}
