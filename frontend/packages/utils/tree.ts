/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { TreeSelectOption } from '~/types'

/**
 * 平铺数据转成树
 * 父不在 list 里的孤儿提升到根，不丢数据；成环的节点丢弃并在开发期 console.warn；显式传 parentId 时只取其直属子级、不提升
 * 祖先链上再遇到同一 basicId 即截断为叶子，防重复 id 自引用时递归爆栈
 */
export function listToTree<T extends { basicId: string, parentId?: string, children?: T[] }>(
  list: T[],
  parentId: string | null = null,
): T[] {
  const knownIds = new Set(list.map(item => item.basicId))
  const mountedIds = new Set<string>()

  const asLeaf = (node: T): T => {
    const { children: _children, ...rest } = node
    return rest as T
  }

  const attach = (nodes: T[], ancestors: Set<string>): T[] =>
    nodes.map((node) => {
      mountedIds.add(node.basicId)
      // 祖先链上已出现过同一 basicId：继续下钻会在同一集合上无限重入
      if (ancestors.has(node.basicId)) {
        return asLeaf(node)
      }
      const children = attach(
        list.filter(item => item.parentId === node.basicId),
        new Set(ancestors).add(node.basicId),
      )
      return children.length > 0
        ? { ...node, children }
        : asLeaf(node)
    })

  if (parentId !== null) {
    return attach(list.filter(item => item.parentId === parentId), new Set([parentId]))
  }

  const roots = list.filter(item => !item.parentId || !knownIds.has(item.parentId))
  const tree = attach(roots, new Set())

  if (import.meta.env.DEV && mountedIds.size < knownIds.size) {
    const dropped = [...knownIds].filter(id => !mountedIds.has(id))
    console.warn(
      `[listToTree] 以下节点的 parentId 构成环，无法挂到任何根上，已被丢弃：${dropped.join(', ')}`,
    )
  }

  return tree
}

/**
 * 平铺的关联选项（value + parentValue）转成树形下拉选项
 * 上级为空、为 0 或不在选项里的作为根挂出，不丢数据；disabled 的节点照常展示层级但不可选
 */
export function relationOptionsToTree(
  options: ReadonlyArray<{ label: string, value: string, parentValue?: null | string, disabled?: boolean }>,
): TreeSelectOption[] {
  interface Node { basicId: string, parentId?: string, label: string, disabled?: boolean, children?: Node[] }
  const nodes: Node[] = options.map(option => ({
    basicId: option.value,
    parentId: option.parentValue ?? undefined,
    label: option.label,
    disabled: option.disabled,
  }))
  const toOption = (node: Node): TreeSelectOption => ({
    label: node.label,
    value: node.basicId,
    ...(node.disabled
      ? { disabled: true }
      : {}),
    ...(node.children?.length
      ? {
          children: node.children.map(toOption),
        }
      : {}),
  })
  return listToTree(nodes).map(toOption)
}

/**
 * 将树形结构转换为平铺数据
 */
export function treeToList<T extends { children?: T[] }>(tree: T[]): Omit<T, 'children'>[] {
  const result: Omit<T, 'children'>[] = []
  const traverse = (nodes: T[]) => {
    for (const node of nodes) {
      const { children, ...rest } = node
      result.push(rest as Omit<T, 'children'>)
      if (children?.length)
        traverse(children)
    }
  }
  traverse(tree)
  return result
}

/**
 * 在树中查找节点
 */
export function findTreeNode<T extends { basicId: string, children?: T[] }>(
  tree: T[],
  basicId: string,
): T | null {
  for (const node of tree) {
    if (node.basicId === basicId)
      return node
    if (node.children) {
      const found = findTreeNode(node.children, basicId)
      if (found)
        return found
    }
  }
  return null
}

/**
 * 按关键字过滤树，保留命中的节点和其祖先链路
 * children 一律写回过滤后的结果，父级命中也不带出未命中的子级
 * 过滤后无子级的节点删掉 children 字段，免得树控件把空 children 当成可展开
 */
export function filterTree<T extends { children?: T[] }>(
  tree: T[],
  keyword: string,
  matcher?: (node: T, normalizedKeyword: string) => boolean,
): T[] {
  const normalizedKeyword = keyword.trim().toLowerCase()
  if (!normalizedKeyword) {
    return tree
  }

  const defaultMatcher = (node: T) =>
    JSON.stringify(node).toLowerCase().includes(normalizedKeyword)

  const match = matcher ?? defaultMatcher

  const dfs = (nodes: T[]): T[] => {
    return nodes.reduce<T[]>((acc, node) => {
      const children = Array.isArray(node.children)
        ? dfs(node.children)
        : []
      if (match(node, normalizedKeyword) || children.length > 0) {
        if (children.length > 0) {
          acc.push({ ...node, children } as T)
        }
        else {
          const { children: _children, ...rest } = node
          acc.push(rest as T)
        }
      }
      return acc
    }, [])
  }

  return dfs(tree)
}

/**
 * 获取节点的所有父节点 id
 * parentId 由人工维护可能成环，上溯时记录走过的节点，命中环即截断，返回已求得的那段
 */
export function getParentIds<T extends { basicId: string, parentId?: string }>(
  list: T[],
  basicId: string,
): string[] {
  const map = new Map(list.map(item => [item.basicId, item]))
  const result: string[] = []
  const visited = new Set<string>([basicId])
  let current = map.get(basicId)
  while (current?.parentId) {
    const { parentId } = current
    if (visited.has(parentId)) {
      break
    }
    visited.add(parentId)
    result.unshift(parentId)
    current = map.get(parentId)
  }
  return result
}
