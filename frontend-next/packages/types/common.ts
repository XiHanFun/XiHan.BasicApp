// ==================== 通用类型 ====================
//
// 这里只放**当前真有消费者**的通用类型。
// 分页契约不在此处：真源是 `~/types/contracts` 的 PageResult（{ items, page: PageResultMetadata }，
// 与后端 PageResultDtoBase 对齐）。本文件曾有一份遗留的 PageResult（{ items, total, page, pageSize }），
// 形状已与后端分叉，且因 `packages/types/index.ts` 导出 './common' 而不导出 './contracts'，
// `import { PageResult } from '~/types'` 拿到的正是错的那份——已删除，勿再添加。

export interface ApiResponse<T = unknown> {
  code: number | string
  message: string
  data: T
  isSuccess: boolean
  traceId?: string
  timestamp?: string
}

export type FrontendRequestLogStatus = 'pending' | 'success' | 'error'

export interface FrontendRequestLog {
  requestId: string
  method: string
  url: string
  startedAt: number
  finishedAt?: number
  duration?: number
  status: FrontendRequestLogStatus
  statusCode?: number
  responseCode?: number | string
  message?: string
  traceId?: string
}

/** 下拉选项 */
export interface SelectOption {
  label: string
  value: string | number
  disabled?: boolean
}

/** 树形下拉选项 */
export interface TreeSelectOption {
  label: string
  value: string | number
  disabled?: boolean
  children?: TreeSelectOption[]
}

/**
 * 顶栏工具的位置：auto 宽屏放顶栏、窄屏 / 内容最大化 / 顶栏隐藏 / 全屏内容布局时悬浮；
 * header 固定顶栏（顶栏放不下或不显示时仍回落悬浮）；floating 固定悬浮；hidden 不显示
 */
export type WidgetPlacement = 'auto' | 'header' | 'floating' | 'hidden'

/** 偏好设置入口的位置：入口不能没有，故没有 hidden */
export type PreferenceEntryPlacement = Exclude<WidgetPlacement, 'hidden'>
