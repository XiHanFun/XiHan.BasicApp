/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Tone } from '@xihan-ui/core'

/**
 * 操作落进下拉菜单时的条目语气：只有破坏性（danger）与停用类（warning）着色，
 * 其余与普通条目同档——菜单里一片彩字就分不出哪条危险了
 */
export function actionMenuTone(tone: Tone | undefined): Tone | undefined {
  return tone === 'danger' || tone === 'warning'
    ? tone
    : undefined
}
