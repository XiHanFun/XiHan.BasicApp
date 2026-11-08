/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { ref } from 'vue'
import { toast } from '~/composables'
import { ApiError } from '~/request'

/**
 * 日志详情抽屉：先用列表行垫上，再取详情补全
 * 垫上是为了抽屉立即有内容；取不到详情就留着列表行并提示
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
      toast.danger(error instanceof ApiError
        ? error.message
        : failedMessage())
    }
    finally {
      loading.value = false
    }
  }

  return { visible, loading, record, open }
}
