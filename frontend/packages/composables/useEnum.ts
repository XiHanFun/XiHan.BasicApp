import { ref, computed } from 'vue'
import type { ComputedRef } from 'vue'
import { enumMetadataApi, type EnumItem } from '@/api'

const enumCache = ref<Map<string, EnumItem[]>>(new Map())
let fetchPromise: Promise<void> | null = null

export function useEnum(enumTypeName: string): {
  options: ComputedRef<Array<{ label: string, value: number }>>
  loading: ReturnType<typeof ref<boolean>>
  error: ReturnType<typeof ref<string | null>>
  refresh: () => Promise<void>
} {
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function refresh() {
    if (enumCache.value.has(enumTypeName)) {
      return
    }

    loading.value = true
    error.value = null
    try {
      if (!fetchPromise) {
        fetchPromise = enumMetadataApi.getAll().then((all) => {
          const map = new Map<string, EnumItem[]>()
          for (const meta of all) {
            map.set(meta.enumTypeName, meta.items)
          }
          enumCache.value = map
        }).finally(() => { fetchPromise = null })
      }
      await fetchPromise
    } catch (_e) {
      error.value = '枚举加载失败'
    } finally {
      loading.value = false
    }
  }

  // Auto-fetch on first call
  refresh()

  const options = computed(() =>
    (enumCache.value.get(enumTypeName) ?? []).map(item => ({
      label: item.displayName,
      value: item.value,
    })),
  )

  return { options, loading, error, refresh }
}
