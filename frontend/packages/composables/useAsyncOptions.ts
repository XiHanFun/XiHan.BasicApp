import type { ShallowRef } from 'vue'
import { shallowRef } from 'vue'
import { i18n } from '~/locales'
import { toast } from './ui-service'

/** 异步选项加载器（如代码生成的外键选项接口）；按函数引用去重 */
export type AsyncOptionsLoader<T> = () => Promise<ReadonlyArray<T>>

/** 在途请求：同一加载器并发调用只发一次，结束即移除（不跨页面缓存，免得关联数据改了还读旧的） */
const inflight = new Map<AsyncOptionsLoader<unknown>, Promise<ReadonlyArray<unknown>>>()

function reasonOf(error: unknown): string {
  return error instanceof Error && error.message ? error.message : String(error)
}

/**
 * 调用选项加载器（并发去重）。
 *
 * 同一页面里表单下拉与列表（显示名、搜索下拉、导入反查）往往同时要同一份选项，
 * 在途时共用一次请求；失败就地提示一次再抛出，空下拉不能无声无息。
 */
export function loadAsyncOptions<T>(loader: AsyncOptionsLoader<T>): Promise<ReadonlyArray<T>> {
  let pending = inflight.get(loader) as Promise<ReadonlyArray<T>> | undefined
  if (!pending) {
    const request = loader()
    pending = request
      .catch((error: unknown) => {
        toast.danger(i18n.global.t('component.async_options.load_failed', { reason: reasonOf(error) }))
        throw error
      })
      .finally(() => {
        inflight.delete(loader)
      })
    inflight.set(loader, pending)
  }
  return pending
}

/**
 * 响应式异步选项：调用即加载，未加载或失败时为空数组（失败已在 loadAsyncOptions 里提示）。
 */
export function useAsyncOptions<T>(loader: AsyncOptionsLoader<T>): Readonly<ShallowRef<ReadonlyArray<T>>> {
  const options = shallowRef<ReadonlyArray<T>>([])
  loadAsyncOptions(loader)
    .then((loaded) => {
      options.value = loaded
    })
    .catch(() => undefined)
  return options
}
