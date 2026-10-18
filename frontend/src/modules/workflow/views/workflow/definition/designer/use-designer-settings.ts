/**
 * 流程设计器的设置（主题、小地图、吸附、自动布局的方向与间距）按用户存进用户设置，换浏览器、换电脑都在。
 *
 * 只存用户在设置对话框里改过的条目；没改过的取这里的缺省，再一并赋给设计器的 settings。
 * 用户清空一项时回到这里的缺省。设计器的设置里眼下没有密钥类条目（secret），以后有了要在存之前滤掉。
 */

import type { DesignerSettings, FlowDesignerEventMap, XhFlowDesigner } from '~/designers/flow'
import { useUserSettingSync } from '~/components/schema'

/** 用户设置里放设计器设置的分区；与所在页面的列设置、搜索设置同一条记录 */
const SETTINGS_SECTION = 'designer'

/** 宿主给的缺省：自动布局自左而右，与 XiHan 工作流引擎预设的端口方向一致；间距照旧版设计器，层间 88、同层 64 */
export const FLOW_DESIGNER_DEFAULT_SETTINGS: DesignerSettings = {
  'flow.layoutDirection': 'LR',
  'flow.layerGap': 88,
  'flow.nodeGap': 64,
}

type SettingValue = DesignerSettings[string]

function isSettings(value: unknown): value is DesignerSettings {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

/**
 * 把设计器的设置接到用户设置上。
 * @param pageCode 所在页面的设置键。
 * @returns attach 在设计器挂上后调用一次；onSettingsChange 接设计器的 settings-change 事件。
 */
export function useFlowDesignerSettings(pageCode: string) {
  const sync = useUserSettingSync(pageCode)
  let designer: XhFlowDesigner | null = null
  let changed: Record<string, SettingValue> = {}
  const removed = new Set<string>()

  function apply(): void {
    if (designer)
      designer.settings = { ...FLOW_DESIGNER_DEFAULT_SETTINGS, ...changed }
  }

  /**
   * 先按缺省赋上，取回用户存过的那份后再赋一次；取回之前用户已经改过的条目以本次改的为准。
   * @param element 设计器元素。
   */
  function attach(element: XhFlowDesigner): void {
    designer = element
    apply()
    void sync.hydrate<unknown>(SETTINGS_SECTION).then((stored) => {
      if (!isSettings(stored))
        return
      const kept = Object.fromEntries(Object.entries(stored).filter(([key]) => !removed.has(key)))
      changed = { ...kept, ...changed }
      apply()
    })
  }

  /**
   * 用户改了一条设置：记下并存回用户设置；清空的一项删掉，回到缺省。
   * @param event 设计器的 settings-change 事件。
   */
  function onSettingsChange(event: Event): void {
    const { key, value } = (event as FlowDesignerEventMap['settings-change']).detail
    if (value === undefined) {
      const { [key]: _, ...rest } = changed
      changed = rest
      removed.add(key)
      apply()
    }
    else {
      changed = { ...changed, [key]: value }
      removed.delete(key)
    }
    sync.save(SETTINGS_SECTION, changed)
  }

  return { attach, onSettingsChange }
}
