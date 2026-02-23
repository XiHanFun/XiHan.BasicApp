// ==================== 标签页类型 ====================

export interface TabItem {
  key: string
  title: string
  path: string
  pinned?: boolean
  closable: boolean
  meta?: {
    icon?: string
    [key: string]: unknown
  }
}
