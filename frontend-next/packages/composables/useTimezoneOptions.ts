import { computed, ref } from 'vue'
import { useAppContext } from '~/stores/app-context'

export interface TimezoneOption {
  /** IANA 时区标识，保存值 */
  value: string
  /** 下拉展示文本：`{IANA ID} (UTC±hh:mm)` */
  label: string
  /** 相对 UTC 的基础偏移分钟数 */
  offsetMinutes: number
}

/**
 * 顶栏下拉用的常用时区。
 *
 * 顶栏是一个点开即选的下拉，塞进四百多条完整目录没法用；这里只挑常用的几条，
 * 但仍从同一份后端目录里筛，不另行硬编码名称与偏移——避免出现「顶栏写死一套、
 * 选择器另一套」的分歧。目录里没有的条目自动跳过。
 */
export const COMMON_TIMEZONE_IDS = [
  'UTC',
  'Asia/Shanghai',
  'Asia/Tokyo',
  'Europe/London',
  'America/New_York',
  'America/Los_Angeles',
] as const

/** 模块级缓存：目录只随运行环境变化，一个会话拉一次即可，多个组件共用 */
const options = ref<TimezoneOption[]>([])
const loading = ref(false)
let pending: Promise<void> | null = null

function formatOffset(offsetMinutes: number): string {
  const sign = offsetMinutes < 0 ? '-' : '+'
  const abs = Math.abs(offsetMinutes)
  const hours = String(Math.floor(abs / 60)).padStart(2, '0')
  const minutes = String(abs % 60).padStart(2, '0')
  return `UTC${sign}${hours}:${minutes}`
}

async function load(): Promise<void> {
  if (options.value.length > 0) {
    return
  }
  if (pending) {
    return pending
  }
  loading.value = true
  pending = (async () => {
    try {
      const list = await useAppContext().apis.timeZoneApi.options()
      // 只取 IANA ID + 偏移：后端 displayName 自带 `(UTC+08:00)` 前缀且拖着一长串城市名，
      // 拼进来既重复又冗长（Asia/Shanghai — (UTC+08:00) (UTC+08:00) 北京, 重庆, ...）
      options.value = list.map(item => ({
        value: item.id,
        label: `${item.id} (${formatOffset(item.baseUtcOffsetMinutes)})`,
        offsetMinutes: item.baseUtcOffsetMinutes,
      }))
    }
    catch {
      // 静默：调用方各自决定空目录时的兜底展示
      options.value = []
    }
    finally {
      loading.value = false
      pending = null
    }
  })()
  return pending
}

/**
 * 时区选项的单一来源：顶栏、个人中心、编号规则共用。
 *
 * 目录取自后端，因为它筛掉了服务端无法解析的时区——前端自己用
 * `Intl.supportedValuesOf` 枚举出来的 ID 未必能在服务端保存成功。
 */
export function useTimezoneOptions() {
  /** 常用时区（顶栏下拉用），按 COMMON_TIMEZONE_IDS 的顺序取 */
  const commonOptions = computed<TimezoneOption[]>(() => {
    const map = new Map(options.value.map(option => [option.value, option]))
    return COMMON_TIMEZONE_IDS
      .map(id => map.get(id))
      .filter((option): option is TimezoneOption => Boolean(option))
  })

  /**
   * 在完整目录基础上补上一个当前值。
   * 历史数据可能来自另一操作系统，其 ID 不在本机目录内；补进去只为无损展示，
   * 提交时仍由后端再次校验。
   */
  function withCurrent(current?: null | string): TimezoneOption[] {
    const id = current?.trim()
    if (!id || options.value.some(option => option.value === id)) {
      return options.value
    }
    return [{ value: id, label: id, offsetMinutes: 0 }, ...options.value]
  }

  return {
    options,
    commonOptions,
    loading,
    ensureLoaded: load,
    withCurrent,
  }
}
