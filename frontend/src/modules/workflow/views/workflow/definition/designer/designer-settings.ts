import type { DesignerSettings } from '~/designers/flow'
import { FLOW_SETTING_KEYS } from '~/designers/flow-designer'

/** 流程设计器设置的宿主缺省：自动布局自左而右，与 XiHan 工作流引擎预设的端口方向一致；间距照旧版设计器，层间 88、同层 64 */
export const FLOW_DESIGNER_DEFAULT_SETTINGS: DesignerSettings = {
  [FLOW_SETTING_KEYS.layoutDirection]: 'LR',
  [FLOW_SETTING_KEYS.layerGap]: 88,
  [FLOW_SETTING_KEYS.nodeGap]: 64,
}
