import type { AppMenuOption } from '~/types'
import { describe, expect, it } from 'vitest'
import { menuAncestors, revealExpanded } from './menu-expansion'

function option(key: string, children?: AppMenuOption[]): AppMenuOption {
  return {
    key,
    label: key,
    children,
  }
}

/** 一级 > 二级 > 三级 > 在线用户；二级下还有一条外链；顶层另有工作台与帮助中心 > 关于 */
const tree: AppMenuOption[] = [
  option('/workbench/dashboard'),
  option('/custom.lvl1', [
    option('/custom.lvl2', [
      option('/custom.lvl3', [
        option('/identity/online-users'),
      ]),
      option('/custom.lvl-link'),
    ]),
  ]),
  option('/custom.about', [
    option('/about/project'),
  ]),
]

describe('menuAncestors', () => {
  it('给出从根到当前页的各级分支，不含当前页自己', () => {
    expect(menuAncestors(tree, '/identity/online-users')).toEqual(['/custom.lvl1', '/custom.lvl2', '/custom.lvl3'])
    expect(menuAncestors(tree, '/custom.lvl-link')).toEqual(['/custom.lvl1', '/custom.lvl2'])
    expect(menuAncestors(tree, '/about/project')).toEqual(['/custom.about'])
  })

  it('当前页在顶层即空；分支自己也算得出路径；不在树里即 null', () => {
    expect(menuAncestors(tree, '/workbench/dashboard')).toEqual([])
    expect(menuAncestors(tree, '/custom.lvl3')).toEqual(['/custom.lvl1', '/custom.lvl2'])
    expect(menuAncestors(tree, '/nowhere')).toBeNull()
    expect(menuAncestors([], '/workbench/dashboard')).toBeNull()
  })
})

describe('revealExpanded', () => {
  const path = ['/custom.lvl1', '/custom.lvl2', '/custom.lvl3']

  it('手风琴下换成通往当前页的这条路径', () => {
    expect(revealExpanded(['/custom.about'], path, true)).toEqual(path)
    expect(revealExpanded([], ['/custom.about'], true)).toEqual(['/custom.about'])
  })

  it('可多开时在已展开的基础上补上这条路径，不重复', () => {
    expect(revealExpanded(['/custom.about', '/custom.lvl1'], path, false)).toEqual([
      '/custom.about',
      '/custom.lvl1',
      '/custom.lvl2',
      '/custom.lvl3',
    ])
  })

  it('当前页在顶层时原样留着，不收用户自己点开的分支', () => {
    const expanded = ['/custom.about']
    expect(revealExpanded(expanded, [], true)).toEqual(['/custom.about'])
    expect(revealExpanded(expanded, [], false)).toEqual(['/custom.about'])
    expect(revealExpanded(expanded, [], true)).not.toBe(expanded)
  })
})
