import type { MaybeRefOrGetter } from 'vue'
import type { ResponseOf } from '~/contracts'
import { ref, toValue, watch } from 'vue'
import { workbench as workbenchApi } from '~/contracts'
import { ApiError } from '~/request'
import { getDisplayTimeZone, utcOffsetMinutes } from '~/utils'

/** 一个小组件的数据（后端 workbench.widget-data，按形态只填其中几项） */
export type WidgetData = ResponseOf<'workbench.widget-data'>
/** 调用方看得到的小组件（键与取数形态） */
export type WidgetEntry = ResponseOf<'workbench.widgets'>['items'][number]

/** 应用时区（界面显示时间用的那个）此刻相对 UTC 的分钟数（东八区为 480）：后端据此算「今天」与按日分桶 */
export function viewerOffset(date: Date = new Date()): number {
  return utcOffsetMinutes(getDisplayTimeZone(), date)
}

/** 错误转成一句文案：后端的问题详情优先 */
export function errorText(error: unknown): string {
  if (error instanceof ApiError)
    return error.detail
  return error instanceof Error ? error.message : String(error ?? '')
}

/** 取一个小组件的数据；天数变了重取，失败只记文案不抛出（看板上其它小组件照常） */
export function useWidgetData(key: string, days: MaybeRefOrGetter<number> = 30) {
  const data = ref<WidgetData | null>(null)
  const loading = ref(true)
  const error = ref<string | null>(null)
  let seq = 0

  async function load() {
    const current = ++seq
    loading.value = true
    error.value = null
    try {
      const result = await workbenchApi.widgetData({ key, days: toValue(days), offset: viewerOffset() })
      if (current === seq)
        data.value = result
    }
    catch (cause) {
      if (current === seq) {
        data.value = null
        error.value = errorText(cause)
      }
    }
    finally {
      if (current === seq)
        loading.value = false
    }
  }

  watch(() => toValue(days), () => void load(), { immediate: true })
  return { data, loading, error, reload: load }
}

/** 序列里按键取一条的值（没有这条为空数组） */
export function lineOf(data: WidgetData | null | undefined, key: string): number[] {
  return data?.series?.lines.find(line => line.key === key)?.values ?? []
}
