/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Tone } from '@xihan-ui/core'
import { XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { h } from 'vue'

/**
 * 列表单元格里的状态标签（SchemaPage 字段的 render）
 * 后端没有枚举元数据接口，页面按契约字面量自己给文案与语气
 */
export function renderTag(label: string, tone?: Tone) {
  return h(XhTagRoot, { variant: 'subtle', size: 'sm', tone }, () => h(XhTagLabel, null, () => label))
}
