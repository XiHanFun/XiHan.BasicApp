/**
 * 将平铺数据转换为树形结构
 *
 * 三条口径在此写明，避免调用方各自猜测：
 *
 * 1. **孤儿节点提升到根**。parentId 指向的节点不在 list 里（父被删除，或分页 / 权限过滤后
 *    父不在本批返回集合中）时，把该节点当作根节点挂出来，而不是静默丢弃。依据：树控件里
 *    「凭空消失」的数据用户完全无从感知——接口明明返回了，页面上看不到也搜不到；位置不对
 *    至少可见、可搜索、可操作。这也与同文件 filterTree 的口径一致：过滤只做筛选，命中即保留
 *    并带上祖先链路，不会吞掉本该出现的节点。
 * 2. **环上的节点仍然丢弃**。parentId 成环（含自引用）时父确实存在，只是这组数据构不成树，
 *    把它们提升等于凭空造出一个假根。这属于数据配错而非分页缺片，改为开发期 console.warn 报出
 *    未挂载的 id，与 getParentIds 对环的处置（就地截断而非假装无环）保持同一态度。
 * 3. **显式传入 parentId 时不做提升**。那是「只构建这棵子树」的语义，只取该父下的直属子级。
 *
 * 另外，递归沿祖先链记录已出现过的 basicId：列表里存在重复 basicId 且其中一份自引用时
 * （如 `[{ basicId: 'a' }, { basicId: 'a', parentId: 'a' }]`），原实现会用同一个 id 反复
 * 递归同一集合，直接 RangeError 爆栈、整棵树渲染失败。祖先链上再次遇到同一 id 即截断为叶子。
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
      return children.length > 0 ? { ...node, children } : asLeaf(node)
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
 *
 * 节点的 children 一律写回**过滤后**的结果：原实现只在过滤后子集非空时才覆盖 children，
 * 父节点自身命中而子节点全不命中时，`...node` 会把那份**未过滤**的原始 children 整棵带出来。
 * 于是同一次搜索出现非单调结果：命中父级 + 0 个子级 → 显示全部 N 个子级；
 * 命中父级 + 1 个子级 → 只显示那 1 个。搜菜单时命中一个分组，其下全部子菜单跟着出现，
 * 看起来像没过滤。这里统一为「只保留命中链路」，过滤后无子级的节点按叶子处理、删掉 children 字段
 * （与 listToTree 的叶子形态一致，避免树控件把空 children 误判为可展开）。
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
 *
 * 沿 parentId 逐级上溯时记录走过的节点：菜单、部门、字典这些树的 parentId 由人工维护，
 * 一旦配成环（a 的父是 b、b 的父是 a），无记录的上溯会在环上无限交替取值，
 * result 一直增长到内存耗尽，主线程直接卡死。命中环时就地截断，返回已求得的那段。
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
