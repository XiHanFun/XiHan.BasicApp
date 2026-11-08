/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { VNodeChild } from 'vue'
import { XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { h } from 'vue'

/**
 * 请求方法列：方法原文放进中性标签
 * 不按 HttpMethodType 映射：OPTIONS / HEAD 等不在选项里，会显示为空
 * 不着色：语气色留给同一行的访问结果与状态码
 */
export function renderHttpMethod(method: string | null | undefined): VNodeChild {
  return method
    ? h(XhTagRoot, { variant: 'subtle', tone: 'neutral' }, () => h(XhTagLabel, () => method))
    : '-'
}
