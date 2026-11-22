/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { SchemaSortRule } from './types'
import { onScopeDispose, ref } from 'vue'
import { storage } from '~/utils'
import { useUserSettingSync } from './useUserSettingSync'

/**
 * 预设快照 —— 捕获一套可复用的列表状态
 */
export interface PresetSnapshot {
  /** 筛选条件 */
  filters: Record<string, unknown>
  /** 多字段排序（数组顺序即优先级） */
  sorts?: SchemaSortRule[]
  /** 每页数量 */
  pageSize?: number
}

/**
 * 条件预设（运行时数据，存 localStorage）
 */
export interface ConditionPreset {
  /** 预设码（唯一） */
  code: string
  /** 预设名 */
  name: string
  /** 快照 */
  snapshot: PresetSnapshot
  /** 是否默认 */
  isDefault?: boolean
}

// 本地存储键前缀、同步段名 views 与预设码前缀 view- 是已存数据的格式：改了就读不回用户存下的预设
const STORAGE_PREFIX = 'xh:views:'
const SYNC_SECTION = 'views'
const CODE_PREFIX = 'view-'

/** 条件预设的状态：增删改查与默认设置，按 pageCode 持久化 */
export function usePresetManager(pageCode: string) {
  const storageKey = `${STORAGE_PREFIX}${pageCode}`
  const sync = useUserSettingSync(pageCode)

  const presets = ref<ConditionPreset[]>(storage.get<ConditionPreset[]>(storageKey) ?? [])
  const activeCode = ref<string | undefined>(presets.value.find(p => p.isDefault)?.code)

  // 跨端同步：开着同步以后端那份为准（等它回来再渲染），其它设备推来时实时应用，二者都写回本地
  // 空数组是用户把预设删光了，同样采用
  const remote = sync.bindSection<ConditionPreset[]>(SYNC_SECTION, {
    accept: (value): value is ConditionPreset[] => Array.isArray(value),
    apply: (list) => {
      presets.value = list
      activeCode.value = list.find(p => p.isDefault)?.code
    },
    writeLocal: list => storage.set(storageKey, list),
  })
  onScopeDispose(remote.dispose)

  function persist() {
    storage.set(storageKey, presets.value)
    sync.save(SYNC_SECTION, presets.value)
  }

  /** 生成预设码（基于已有数量，避免依赖随机数） */
  function nextCode(): string {
    let index = presets.value.length + 1
    let code = `${CODE_PREFIX}${index}`
    const existing = new Set(presets.value.map(p => p.code))
    while (existing.has(code)) {
      index += 1
      code = `${CODE_PREFIX}${index}`
    }
    return code
  }

  /** 新增预设 */
  function addPreset(name: string, snapshot: PresetSnapshot): ConditionPreset {
    const preset: ConditionPreset = {
      code: nextCode(),
      name,
      snapshot,
    }
    presets.value = [...presets.value, preset]
    persist()
    return preset
  }

  /** 更新预设快照 */
  function updatePreset(code: string, snapshot: PresetSnapshot) {
    const target = presets.value.find(p => p.code === code)
    if (target) {
      target.snapshot = snapshot
      persist()
    }
  }

  /** 删除预设 */
  function removePreset(code: string) {
    presets.value = presets.value.filter(p => p.code !== code)
    if (activeCode.value === code) {
      activeCode.value = undefined
    }
    persist()
  }

  /** 设为默认（唯一）；传 null 取消默认 */
  function setDefault(code: null | string) {
    for (const p of presets.value) {
      p.isDefault = p.code === code
    }
    persist()
  }

  /** 套用预设，返回其快照（由调用方落地到表格状态） */
  function applyPreset(code: string): PresetSnapshot | undefined {
    const target = presets.value.find(p => p.code === code)
    if (!target) {
      return undefined
    }
    activeCode.value = code
    return target.snapshot
  }

  return {
    /** 首屏能否渲染：开着同步时等后端那份回来 */
    ready: remote.ready,
    /** 后端那份处理完（或本来就不用等）时 resolve：首次加载据此决定用不用默认预设 */
    settled: remote.settled,
    presets,
    activeCode,
    addPreset,
    updatePreset,
    removePreset,
    setDefault,
    applyPreset,
  }
}
