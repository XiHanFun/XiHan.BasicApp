/**
 * packages/utils/tree.ts 单元测试。
 *
 * 职责边界：扁平表 ↔ 树的双向转换、树内查找、按关键字过滤（含祖先链保留）、父级 id 链回溯。
 * 重点覆盖树工具最容易出错的输入：空集合、孤儿节点（父不存在）、重复 id、自引用、
 * 空字符串 parentId。其中若干条用例锁定的是源码**当前**行为（含缺陷），已在标题中标注。
 */
import { describe, expect, it, vi } from 'vitest'
import { filterTree, findTreeNode, getParentIds, listToTree, treeToList } from './tree'

interface FlatNode {
  basicId: string
  parentId?: string
  name?: string
  children?: FlatNode[]
}

interface TreeNode {
  basicId: string
  name?: string
  children?: TreeNode[]
}

describe('listToTree', () => {
  it('空列表返回空数组', () => {
    expect(listToTree<FlatNode>([])).toEqual([])
  })

  it('无 parentId 的节点作为根，逐层挂载子节点', () => {
    const list: FlatNode[] = [
      { basicId: 'a' },
      { basicId: 'a-1', parentId: 'a' },
      { basicId: 'a-1-1', parentId: 'a-1' },
      { basicId: 'b' },
    ]

    expect(listToTree(list)).toEqual([
      {
        basicId: 'a',
        children: [
          { basicId: 'a-1', parentId: 'a', children: [{ basicId: 'a-1-1', parentId: 'a-1' }] },
        ],
      },
      { basicId: 'b' },
    ])
  })

  it('叶子节点不带空的 children 字段，避免前端组件误判为可展开', () => {
    const [leaf] = listToTree<FlatNode>([{ basicId: 'a' }])
    expect(leaf && 'children' in leaf).toBe(false)
  })

  it('不修改入参，原始扁平项不会被写入 children', () => {
    const list: FlatNode[] = [{ basicId: 'a' }, { basicId: 'a-1', parentId: 'a' }]
    listToTree(list)
    expect(list[0]).toEqual({ basicId: 'a' })
  })

  it('parentId 为空串的节点与无 parentId 一样被当作根节点', () => {
    expect(listToTree<FlatNode>([{ basicId: 'x', parentId: '' }])).toEqual([
      { basicId: 'x', parentId: '' },
    ])
  })

  it('孤儿节点（父 id 在列表中不存在）被提升为根节点，而不是静默消失', () => {
    // 回归锚点：原实现的根层筛选只认「没有 parentId」，父 id 悬空的节点既进不了根层，
    // 也没有任何一次递归会拿那个悬空 id 当 parentId，整条数据在接口里存在、树上却看不到也搜不到。
    // 父被删除、或分页 / 权限过滤后父不在本批集合中都会命中，此时提升到根至少保证可见可操作。
    expect(listToTree<FlatNode>([{ basicId: 'x', parentId: 'ghost' }])).toEqual([
      { basicId: 'x', parentId: 'ghost' },
    ])
  })

  it('被提升的孤儿节点仍然带着自己的子树', () => {
    expect(
      listToTree<FlatNode>([
        { basicId: 'x', parentId: 'ghost' },
        { basicId: 'x-1', parentId: 'x' },
      ]),
    ).toEqual([{ basicId: 'x', parentId: 'ghost', children: [{ basicId: 'x-1', parentId: 'x' }] }])
  })

  it('自引用节点的父确实存在（就是自己），不提升为根而是丢弃并告警', () => {
    // 环上的节点父确实存在，只是这组数据构不成树，提升等于凭空造出一个假根 —— 与孤儿区别对待
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})

    expect(listToTree<FlatNode>([{ basicId: 'a', parentId: 'a' }])).toEqual([])
    expect(warn).toHaveBeenCalledTimes(1)
    expect(warn.mock.calls[0]?.[0]).toContain('a')
  })

  it('互为父子的两个节点全部被丢弃，环不会挂到任何根上，并在开发期告警', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})

    expect(
      listToTree<FlatNode>([
        { basicId: 'a', parentId: 'b' },
        { basicId: 'b', parentId: 'a' },
      ]),
    ).toEqual([])
    expect(warn).toHaveBeenCalledTimes(1)
  })

  it('全部节点都挂上时不产生任何告警', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})

    listToTree<FlatNode>([{ basicId: 'a' }, { basicId: 'a-1', parentId: 'a' }])
    expect(warn).not.toHaveBeenCalled()
  })

  it('重复 basicId 的根节点会让同一批子节点被挂载多份', () => {
    const result = listToTree<FlatNode>([
      { basicId: 'a' },
      { basicId: 'a' },
      { basicId: 'c', parentId: 'a' },
    ])

    expect(result).toHaveLength(2)
    expect(result[0]?.children).toEqual([{ basicId: 'c', parentId: 'a' }])
    expect(result[1]?.children).toEqual([{ basicId: 'c', parentId: 'a' }])
  })

  it('重复 id 且其中一份自引用时在祖先链上截断为叶子，不再递归爆栈', () => {
    // 回归锚点：原实现构建第一份 'a' 的 children 时命中第二份 parentId==='a' 的 'a'，
    // 又以同一个 id 递归同一集合，无限重入直到 RangeError: Maximum call stack size exceeded，
    // 整棵树渲染失败。现在祖先链上再次遇到同一 basicId 即停止下钻。
    expect(listToTree<FlatNode>([{ basicId: 'a' }, { basicId: 'a', parentId: 'a' }])).toEqual([
      { basicId: 'a', children: [{ basicId: 'a', parentId: 'a' }] },
    ])
  })

  it('长环挂在真实根下时同样截断，不会无限展开', () => {
    expect(
      listToTree<FlatNode>([
        { basicId: 'root' },
        { basicId: 'a', parentId: 'root' },
        { basicId: 'b', parentId: 'a' },
        { basicId: 'a', parentId: 'b' },
      ]),
    ).toEqual([
      {
        basicId: 'root',
        children: [
          {
            basicId: 'a',
            parentId: 'root',
            children: [{ basicId: 'b', parentId: 'a', children: [{ basicId: 'a', parentId: 'b' }] }],
          },
        ],
      },
    ])
  })

  it('显式传入 parentId 时只构建该子树', () => {
    const list: FlatNode[] = [
      { basicId: 'a' },
      { basicId: 'a-1', parentId: 'a' },
      { basicId: 'b' },
    ]

    expect(listToTree(list, 'a')).toEqual([{ basicId: 'a-1', parentId: 'a' }])
  })
})

describe('treeToList', () => {
  it('空树返回空数组', () => {
    expect(treeToList<TreeNode>([])).toEqual([])
  })

  it('按深度优先前序展开，父节点排在其子孙之前', () => {
    const tree: TreeNode[] = [
      { basicId: 'a', children: [{ basicId: 'a-1', children: [{ basicId: 'a-1-1' }] }] },
      { basicId: 'b' },
    ]

    expect(treeToList(tree).map(item => item.basicId)).toEqual(['a', 'a-1', 'a-1-1', 'b'])
  })

  it('展开结果剥掉 children 字段，只保留节点自身属性', () => {
    const flat = treeToList<TreeNode>([{ basicId: 'a', name: '根', children: [{ basicId: 'a-1' }] }])
    expect(flat[0]).toEqual({ basicId: 'a', name: '根' })
  })

  it('children 为空数组的节点被视为叶子，不进入递归', () => {
    expect(treeToList<TreeNode>([{ basicId: 'a', children: [] }])).toEqual([{ basicId: 'a' }])
  })

  it('与 listToTree 往返后节点集合一致（差异仅在 parentId 字段保留）', () => {
    const list: FlatNode[] = [
      { basicId: 'a' },
      { basicId: 'a-1', parentId: 'a' },
      { basicId: 'a-2', parentId: 'a' },
      { basicId: 'a-1-1', parentId: 'a-1' },
    ]

    const roundTrip = treeToList(listToTree(list)).map(item => item.basicId).sort()
    expect(roundTrip).toEqual(['a', 'a-1', 'a-1-1', 'a-2'])
  })
})

describe('findTreeNode', () => {
  const tree: TreeNode[] = [
    { basicId: 'a', children: [{ basicId: 'a-1', children: [{ basicId: 'target', name: '深层' }] }] },
    { basicId: 'b' },
  ]

  it('命中根层节点时返回原始引用而不是副本', () => {
    expect(findTreeNode(tree, 'b')).toBe(tree[1])
  })

  it('能穿透多层 children 找到深层节点', () => {
    expect(findTreeNode(tree, 'target')?.name).toBe('深层')
  })

  it('id 不存在时返回 null 而不是 undefined', () => {
    expect(findTreeNode(tree, '不存在')).toBeNull()
  })

  it('空树返回 null', () => {
    expect(findTreeNode<TreeNode>([], 'a')).toBeNull()
  })

  it('id 重复时返回深度优先遇到的第一个节点', () => {
    const duplicated: TreeNode[] = [
      { basicId: 'root', children: [{ basicId: 'dup', name: '子层的' }] },
      { basicId: 'dup', name: '根层的' },
    ]

    expect(findTreeNode(duplicated, 'dup')?.name).toBe('子层的')
  })

  it('空字符串 id 不会误命中任何节点', () => {
    expect(findTreeNode(tree, '')).toBeNull()
  })
})

describe('filterTree', () => {
  const tree: TreeNode[] = [
    {
      basicId: 'sys',
      name: '系统管理',
      children: [
        { basicId: 'user', name: '用户管理' },
        { basicId: 'role', name: '角色管理' },
      ],
    },
    { basicId: 'log', name: '日志中心' },
  ]

  it('关键字为空白时原样返回入参引用，不做任何拷贝', () => {
    expect(filterTree(tree, '   ')).toBe(tree)
    expect(filterTree(tree, '')).toBe(tree)
  })

  it('命中子节点时保留其祖先链路', () => {
    const result = filterTree(tree, '角色', node => (node.name ?? '').includes('角色'))

    expect(result).toHaveLength(1)
    expect(result[0]?.basicId).toBe('sys')
    expect(result[0]?.children?.map(child => child.basicId)).toEqual(['role'])
  })

  it('全部落空时返回空数组', () => {
    expect(filterTree(tree, 'zzz', node => (node.name ?? '').includes('zzz'))).toEqual([])
  })

  it('自定义匹配器收到的是去空白并转小写后的关键字', () => {
    const received: string[] = []
    filterTree(tree, '  ABC  ', (_node, normalized) => {
      received.push(normalized)
      return false
    })

    expect(new Set(received)).toEqual(new Set(['abc']))
  })

  it('父节点自身命中而子节点全不命中时按叶子返回，不再把未过滤的原子树带出来', () => {
    // 回归锚点：原实现只在过滤后子集非空时才覆盖 children，此处 children 为空跳过覆盖，
    // `...node` 把那份**未过滤**的原始 children 整棵带出，搜到一个分组就把它下面全部子菜单一起显示。
    // 表现为非单调：命中父级 + 0 个子级 → 显示全部 2 个；命中父级 + 1 个子级 → 只显示 1 个。
    const result = filterTree(tree, '系统', node => node.basicId === 'sys')

    expect(result).toHaveLength(1)
    expect(result[0]?.basicId).toBe('sys')
    expect(result[0] && 'children' in result[0]).toBe(false)
  })

  it('命中父级且命中其中一个子级时只保留那个子级（与上一条构成同一条单调口径）', () => {
    const result = filterTree(tree, '管理', node => (node.name ?? '').includes('管理'))

    expect(result[0]?.children?.map(child => child.basicId)).toEqual(['user', 'role'])
  })

  it('默认匹配器对整棵子树做 JSON 串匹配，子孙的文本会让祖先一并命中', () => {
    const result = filterTree(tree, '角色管理')

    expect(result.map(node => node.basicId)).toEqual(['sys'])
    expect(result[0]?.children?.map(child => child.basicId)).toEqual(['role'])
  })

  it('默认匹配器大小写不敏感', () => {
    const data: TreeNode[] = [{ basicId: 'x', name: 'DashBoard' }]
    expect(filterTree(data, 'dashboard').map(node => node.basicId)).toEqual(['x'])
  })

  it('默认匹配器也会匹配到 basicId 等非展示字段，属已知的过宽命中', () => {
    expect(filterTree(tree, 'log').map(node => node.basicId)).toEqual(['log'])
  })

  it('过滤产生新节点对象，不会改动原树的 children', () => {
    const result = filterTree(tree, '用户', node => (node.name ?? '').includes('用户'))

    expect(result[0]).not.toBe(tree[0])
    expect(tree[0]?.children).toHaveLength(2)
  })

  it('空树在任意关键字下都返回空数组', () => {
    expect(filterTree<TreeNode>([], '任意')).toEqual([])
  })
})

describe('getParentIds', () => {
  const list: FlatNode[] = [
    { basicId: 'a' },
    { basicId: 'a-1', parentId: 'a' },
    { basicId: 'a-1-1', parentId: 'a-1' },
  ]

  it('返回自顶向下排序的父级 id 链，不含自身', () => {
    expect(getParentIds(list, 'a-1-1')).toEqual(['a', 'a-1'])
  })

  it('根节点没有父级，返回空数组', () => {
    expect(getParentIds(list, 'a')).toEqual([])
  })

  it('id 不存在时返回空数组而不是抛错', () => {
    expect(getParentIds(list, '不存在')).toEqual([])
  })

  it('空列表返回空数组', () => {
    expect(getParentIds<FlatNode>([], 'a')).toEqual([])
  })

  it('父级 id 悬空时仍把该 id 计入结果，链路在此中断（当前行为）', () => {
    expect(getParentIds<FlatNode>([{ basicId: 'x', parentId: 'ghost' }], 'x')).toEqual(['ghost'])
  })

  it('parentId 为空串视同无父级', () => {
    expect(getParentIds<FlatNode>([{ basicId: 'x', parentId: '' }], 'x')).toEqual([])
  })

  it('重复 basicId 时以 Map 后写入的那一条为准', () => {
    const duplicated: FlatNode[] = [
      { basicId: 'dup', parentId: 'p1' },
      { basicId: 'dup', parentId: 'p2' },
      { basicId: 'p1' },
      { basicId: 'p2' },
    ]

    expect(getParentIds(duplicated, 'dup')).toEqual(['p2'])
  })

  it('parentId 成环时就地截断，不会死循环', () => {
    // 回归锚点：上溯原本不记录走过的节点，环上两点会被无限交替取到，
    // result 一直增长到内存耗尽、主线程卡死。菜单/部门/字典树的 parentId 由人工维护，
    // 配成环完全可能。去掉 visited 判断后本用例会挂住（而不是失败），这正是当初没法写测试的原因。
    const cyclic: FlatNode[] = [
      { basicId: 'a', parentId: 'b' },
      { basicId: 'b', parentId: 'a' },
    ]

    expect(getParentIds(cyclic, 'a')).toEqual(['b'])
    expect(getParentIds(cyclic, 'b')).toEqual(['a'])
  })

  it('自引用节点不会死循环', () => {
    expect(getParentIds<FlatNode>([{ basicId: 'self', parentId: 'self' }], 'self')).toEqual([])
  })

  it('三节点长环同样截断，返回环打开前已求得的那段', () => {
    const cyclic: FlatNode[] = [
      { basicId: 'a', parentId: 'b' },
      { basicId: 'b', parentId: 'c' },
      { basicId: 'c', parentId: 'a' },
    ]

    expect(getParentIds(cyclic, 'a')).toEqual(['c', 'b'])
  })
})
