import type { DesignerSettings } from '@xihan-ui/designer-shell'
import type { Ref } from 'vue'
import { ref } from 'vue'
import { useUserSettingSync } from '~/components/schema'

/**
 * 设计器的设置（主题、面板布局、工具栏、新手引导走过与否，以及各款自己那一页）按用户存进用户设置，换浏览器、换电脑都在。
 *
 * 只存用户改过的条目；没改过的取宿主给的缺省，再一并赋给设计器的 settings。用户清空一项时回到宿主缺省。
 * 用户那份取回之前 ready 为 false：宿主等它为 true 再挂设计器，设计器第一次铺好时就看得到存过的 help.tourDone。
 * 设计器的设置里眼下没有密钥类条目（secret），以后宿主给了带 secret 条目的扩展，要在存之前滤掉。
 */

/** 用户设置里放设计器设置的分区；与所在页面的列设置、搜索设置同一条记录。 */
const SETTINGS_SECTION = 'designer'

type SettingValue = DesignerSettings[string]

/** 接设置的设计器元素。 */
export interface DesignerSettingsTarget {
  settings: DesignerSettings | undefined
}

export interface DesignerSettingsSync {
  /** 用户存过的那份已取回（取不到也算），宿主据此挂设计器。 */
  readonly ready: Readonly<Ref<boolean>>
  /** 设计器挂上后调用一次：按缺省与用户改过的条目赋上 settings。 */
  attach: (element: DesignerSettingsTarget) => void
  /** 接设计器的 settings-change 事件。 */
  onSettingsChange: (event: Event) => void
}

function isSettings(value: unknown): value is DesignerSettings {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

/**
 * 把设计器的设置接到用户设置上，调用时就开始取回用户存过的那份。
 * @param pageCode 所在页面的设置键。
 * @param defaults 宿主给的缺省，用户没改过的条目取它。
 */
export function useDesignerSettings(pageCode: string, defaults: DesignerSettings = {}): DesignerSettingsSync {
  const sync = useUserSettingSync(pageCode)
  const ready = ref(false)
  let designer: DesignerSettingsTarget | null = null
  let changed: Record<string, SettingValue> = {}
  const removed = new Set<string>()

  function apply(): void {
    if (designer)
      designer.settings = { ...defaults, ...changed }
  }

  // 取回之前用户已经改过或清空的条目以那次操作为准
  void sync.hydrate<unknown>(SETTINGS_SECTION).then((stored) => {
    if (isSettings(stored)) {
      const kept = Object.fromEntries(Object.entries(stored).filter(([key]) => !removed.has(key)))
      changed = { ...kept, ...changed }
    }
    ready.value = true
    apply()
  })

  function attach(element: DesignerSettingsTarget): void {
    designer = element
    apply()
  }

  function onSettingsChange(event: Event): void {
    const { key, value } = (event as CustomEvent<{ key: string, value: SettingValue | undefined }>).detail
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

  return { ready, attach, onSettingsChange }
}
