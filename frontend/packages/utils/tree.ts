/**
 * 将平铺数据转换为树形结构
 */
export function listToTree<T extends { id: string; parentId?: string; children?: T[] }>(
  list: T[],
  parentId: string | null = null,
): T[] {
  return list
    .filter((item) => item.parentId === parentId || (!item.parentId && parentId === null))
    .map((item) => ({
      ...item,
      children: listToTree(list, item.id),
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
      if (children?.length) traverse(children)
    }
  }
  traverse(tree)
  return result
}

/**
 * 在树中查找节点
 */
export function findTreeNode<T extends { id: string; children?: T[] }>(
  tree: T[],
  id: string,
): T | null {
  for (const node of tree) {
    if (node.id === id) return node
    if (node.children) {
      const found = findTreeNode(node.children, id)
      if (found) return found
    }
  }
  return null
}

/**
 * 获取节点的所有父节点 id
 */
export function getParentIds<T extends { id: string; parentId?: string }>(
  list: T[],
  id: string,
): string[] {
  const map = new Map(list.map((item) => [item.id, item]))
  const result: string[] = []
  let current = map.get(id)
  while (current?.parentId) {
    result.unshift(current.parentId)
    current = map.get(current.parentId)
  }
  return result
}
