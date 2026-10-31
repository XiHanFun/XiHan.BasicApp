/** 系统字典显示契约；颜色及图标配置由组件和API共享，状态流转不依赖这些字段。 */
import type { Tone } from '@xihan-ui/core'

export interface DictTagStyle {
  tone?: Tone | null
  backgroundColor?: string | null
  textColor?: string | null
  /** 未配置时默认显示；关闭仅隐藏图标，不清除已保存的自定义选择。 */
  showIcon?: boolean | null
  /** 离线图标ID；未配置时跟随配色默认图标。 */
  icon?: string | null
}
