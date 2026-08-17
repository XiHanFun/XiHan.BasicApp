/**
 * 将平铺数据转换为树形结构
 */
export function listToTree<T extends { basicId: string, parentId?: string, children?: T[] }>(
  list: T[],
  parentId: string | null = null,
): T[] {
  return list
    .filter(item => item.parentId === parentId || (!item.parentId && parentId === null))
    .map(item => ({
      ...item,
      children: listToTree(list, item.basicId),
    }))
    .map((item) => {
      if ((item.children as T[]).length === 0) {
        const { children: _c, ...rest } = item
        return rest as T
      }
      return item
    })
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
      const children = Array.isArray(node.children) ? dfs(node.children) : []
      if (match(node, normalizedKeyword) || children.length > 0) {
        acc.push({
          ...node,
          ...(children.length > 0 ? { children } : {}),
        } as T)
      }
      return acc
    }, [])
  }

  return dfs(tree)
}

/**
 * 获取节点的所有父节点 id
 */
export function getParentIds<T extends { basicId: string, parentId?: string }>(
  list: T[],
  basicId: string,
): string[] {
  const map = new Map(list.map(item => [item.basicId, item]))
  const result: string[] = []
  let current = map.get(basicId)
  while (current?.parentId) {
    result.unshift(current.parentId)
    current = map.get(current.parentId)
  }
  return result
}
