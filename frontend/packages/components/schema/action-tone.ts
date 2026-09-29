import type { Tone } from '@xihan-ui/core'
import type { ActionSchema } from './types'

type ActionType = ActionSchema['type']

/** 操作 Schema 的 type 到按钮 tone 轴的换算（Schema 里的词汇沿用页面既有声明，不改） */
export function actionButtonTone(type: ActionType): Tone {
  switch (type) {
    case 'primary':
      return 'brand'
    case 'error':
      return 'danger'
    case 'info':
    case 'success':
    case 'warning':
      return type
    default:
      return 'neutral'
  }
}

/**
 * 操作落进下拉菜单时的条目语气：只有破坏性（error → danger）与停用类（warning）着色，
 * 其余与普通条目同档——菜单里一片彩字就分不出哪条危险了。
 */
export function actionMenuTone(type: ActionType): Tone | undefined {
  switch (type) {
    case 'error':
      return 'danger'
    case 'warning':
      return 'warning'
    default:
      return undefined
  }
}
