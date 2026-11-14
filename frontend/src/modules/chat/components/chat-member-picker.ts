/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatUserOption } from '../types'

/**
 * 把候选列表交回来的勾选并进已选
 * 列表只管眼前这批候选：不在这批里的已选原样留着，这批里的按勾选增减；新勾的排在后面，选不了的（已在群里）不收
 * @param picked 现有的已选（按勾选先后）
 * @param candidates 眼前这批候选
 * @param chosenIds 列表交回来的勾选
 * @param lockedIds 列出但选不了的用户
 * @returns 新的已选
 */
export function mergePickedUsers(
  picked: readonly ChatUserOption[],
  candidates: readonly ChatUserOption[],
  chosenIds: readonly string[],
  lockedIds: ReadonlySet<string>,
): ChatUserOption[] {
  const visible = new Set(candidates.map(user => user.userId))
  const chosen = new Set(chosenIds)
  const kept = picked.filter(user => !visible.has(user.userId) || chosen.has(user.userId))
  const keptIds = new Set(kept.map(user => user.userId))
  const added = candidates.filter(user =>
    chosen.has(user.userId) && !keptIds.has(user.userId) && !lockedIds.has(user.userId))
  return [
    ...kept,
    ...added,
  ]
}
