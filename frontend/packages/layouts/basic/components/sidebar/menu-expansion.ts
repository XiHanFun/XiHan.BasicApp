/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { AppMenuOption } from '~/types'

/**
 * 从根到 key 的各级分支键（不含 key 自己）：侧栏据此展开到当前页
 * key 在顶层即空数组；不在树里即 null
 */
export function menuAncestors(nodes: readonly AppMenuOption[], key: string, trail: readonly string[] = []): string[] | null {
  for (const node of nodes) {
    if (node.key === key)
      return [...trail]
    if (node.children?.length) {
      const found = menuAncestors(node.children, key, [...trail, node.key])
      if (found)
        return found
    }
  }
  return null
}

/**
 * 当前页换了之后的展开集合：通往当前页的各级分支都要开着
 * 手风琴（同层只开一枝）下直接换成这条路径；否则在已展开的基础上补上这条路径
 * 当前页在顶层（路径为空）时原样留着，不去收用户自己点开的分支
 */
export function revealExpanded(expanded: readonly string[], path: readonly string[], accordion: boolean): string[] {
  if (path.length === 0)
    return [...expanded]
  if (accordion)
    return [...path]
  return [
    ...expanded,
    ...path.filter(key => !expanded.includes(key)),
  ]
}
