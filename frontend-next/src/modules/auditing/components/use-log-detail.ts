import { ref } from 'vue'
import { toast } from '~/composables'
import { ApiError } from '~/request'

/**
 * 日志详情抽屉的状态：先用列表行垫上（抽屉立即有内容），再取详情（多出堆栈、前后值、正文摘要等）；取不到就留着列表行并提示。
 */
export function useLogDetail<TRow extends { id: string }>(fetch: (id: string) => Promise<object>, failedMessage: () => string) {
  const visible = ref(false)
  const loading = ref(false)
  const record = ref<object | null>(null)

  async function open(row: TRow) {
    record.value = row
    visible.value = true
    loading.value = true
    try {
      record.value = await fetch(row.id)
    }
    catch (error) {
      toast.danger(error instanceof ApiError ? error.message : failedMessage())
    }
    finally {
      loading.value = false
    }
  }

  return { visible, loading, record, open }
}
