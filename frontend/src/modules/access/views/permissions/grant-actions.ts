/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { PermissionAction } from '~/contracts'

/**
 * 某个动作的右栏变成 next：移出的回到未设置，移入的落为该动作
 * 授予与拒绝互斥，从另一栏挪进来的就从另一栏移走
 */
export function applyPermissionTransfer(actions: ReadonlyMap<string, PermissionAction>, action: PermissionAction, next: readonly string[]): Map<string, PermissionAction> {
  const keep = new Set(next)
  const result = new Map(actions)
  for (const [code, value] of result) {
    if (value === action && !keep.has(code))
      result.delete(code)
  }
  for (const code of keep)
    result.set(code, action)
  return result
}

/**
 * 授予与拒绝草稿的差量，动作没变的不重复提交
 * 新增或改了动作的分别授予或拒绝，草稿里已没有的当前项撤销
 */
export function diffPermissionGrants(actions: ReadonlyMap<string, PermissionAction>, current: ReadonlyMap<string, PermissionAction>): { grant: string[], deny: string[], revoke: string[] } {
  const grant: string[] = []
  const deny: string[] = []
  for (const [code, action] of actions) {
    if (current.get(code) === action)
      continue
    if (action === 'Grant')
      grant.push(code)
    else
      deny.push(code)
  }
  const revoke = [
    ...current.keys(),
  ].filter(code => !actions.has(code))
  return { grant, deny, revoke }
}
