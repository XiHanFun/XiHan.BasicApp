import type { VNodeChild } from 'vue'
import { XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { h } from 'vue'

/**
 * 日志列表的请求方法列：方法原文放进中性标签。
 *
 * 不按 HttpMethodType 选项映射——OPTIONS / HEAD 等不在搜索选项里，映射后会显示为空；
 * 也不按方法着色，同一行的访问结果、状态码才用语气色，方法再上色就分不清哪列是状态。
 */
export function renderHttpMethod(method: string | null | undefined): VNodeChild {
  return method
    ? h(XhTagRoot, { variant: 'subtle', tone: 'neutral' }, () => h(XhTagLabel, () => method))
    : '-'
}
