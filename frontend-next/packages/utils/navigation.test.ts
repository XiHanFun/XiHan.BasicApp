/**
 * packages/utils/navigation.ts 单元测试。
 *
 * 职责边界：路由 full path 拼接、可见路径存在性判定、以及「点菜单落到第一个可导航子页」的
 * 递归解析。hidden 元信息、redirect 是否指向可见子路由、隐藏父级仍需下钻这几条分支是重点。
 */
import type { NavigableRouteLike } from './navigation'
import { describe, expect, it } from 'vitest'
import {
  resolveFirstNavigableRouteListPath,
  resolveFirstNavigableRoutePath,
  resolveRouteFullPath,
  routeListContainsVisiblePath,
} from './navigation'

describe('resolveRouteFullPath', () => {
  it('路径为空时退回父路径', () => {
    expect(resolveRouteFullPath('', '/system')).toBe('/system')
  })

  it('路径与父路径都为空时退回根路径', () => {
    expect(resolveRouteFullPath('', '')).toBe('/')
    expect(resolveRouteFullPath()).toBe('/')
  })

  it('绝对路径忽略父路径直接返回', () => {
    expect(resolveRouteFullPath('/dashboard', '/system')).toBe('/dashboard')
  })

  it('相对路径拼在父路径之后', () => {
    expect(resolveRouteFullPath('user', '/system')).toBe('/system/user')
  })

  it('父路径尾部斜杠不会拼出双斜杠', () => {
    expect(resolveRouteFullPath('user', '/system/')).toBe('/system/user')
  })

  it('父路径为空时相对路径被补成根下一级', () => {
    expect(resolveRouteFullPath('login', '')).toBe('/login')
  })

  it('多段相对路径中的连续斜杠被折叠成单个', () => {
    expect(resolveRouteFullPath('a//b', '/root')).toBe('/root/a/b')
  })

  it('父路径为单个斜杠时不产生前导双斜杠', () => {
    expect(resolveRouteFullPath('home', '/')).toBe('/home')
  })
})

describe('routeListContainsVisiblePath', () => {
  const routes: NavigableRouteLike[] = [
    {
      path: '/system',
      children: [
        { path: 'user' },
        { path: 'secret', meta: { hidden: true } },
        { path: 'nested', children: [{ path: 'deep' }] },
      ],
    },
    { path: '/hidden-parent', meta: { hidden: true }, children: [{ path: 'child' }] },
  ]

  it('命中可见的一级路径', () => {
    expect(routeListContainsVisiblePath(routes, '/system')).toBe(true)
  })

  it('命中需要拼接父路径的深层可见路径', () => {
    expect(routeListContainsVisiblePath(routes, '/system/nested/deep')).toBe(true)
  })

  it('被 meta.hidden 标记的路径判为不存在', () => {
    expect(routeListContainsVisiblePath(routes, '/system/secret')).toBe(false)
  })

  it('隐藏父级下的可见子路径仍能被找到，隐藏只作用于节点自身', () => {
    expect(routeListContainsVisiblePath(routes, '/hidden-parent/child')).toBe(true)
  })

  it('未定义的路由列表与空列表都返回假', () => {
    expect(routeListContainsVisiblePath(undefined, '/a')).toBe(false)
    expect(routeListContainsVisiblePath([], '/a')).toBe(false)
  })

  it('不存在的路径返回假', () => {
    expect(routeListContainsVisiblePath(routes, '/system/none')).toBe(false)
  })

  it('meta 为 null 的路由按可见处理', () => {
    expect(routeListContainsVisiblePath([{ path: '/a', meta: null }], '/a')).toBe(true)
  })

  it('meta.hidden 为 false 时按可见处理', () => {
    expect(routeListContainsVisiblePath([{ path: '/a', meta: { hidden: false } }], '/a')).toBe(true)
  })

  it('比较的是拼接后的完整路径，父路径不同则不命中', () => {
    expect(routeListContainsVisiblePath(routes, 'user')).toBe(false)
  })
})

describe('resolveFirstNavigableRoutePath', () => {
  it('无子路由时返回自身完整路径', () => {
    expect(resolveFirstNavigableRoutePath({ path: '/about' })).toBe('/about')
  })

  it('逐层下钻到第一个可见叶子', () => {
    const route: NavigableRouteLike = {
      path: '/system',
      children: [{ path: 'user', children: [{ path: 'list' }] }],
    }

    expect(resolveFirstNavigableRoutePath(route)).toBe('/system/user/list')
  })

  it('跳过隐藏子路由，取第一个可见的', () => {
    const route: NavigableRouteLike = {
      path: '/system',
      children: [{ path: 'hidden', meta: { hidden: true } }, { path: 'user' }],
    }

    expect(resolveFirstNavigableRoutePath(route)).toBe('/system/user')
  })

  it('全部子路由都隐藏时停在父级自身路径', () => {
    const route: NavigableRouteLike = {
      path: '/system',
      children: [{ path: 'hidden', meta: { hidden: true } }],
    }

    expect(resolveFirstNavigableRoutePath(route)).toBe('/system')
  })

  it('redirect 指向可见子路由时优先采用 redirect', () => {
    const route: NavigableRouteLike = {
      path: '/system',
      redirect: '/system/role',
      children: [{ path: 'user' }, { path: 'role' }],
    }

    expect(resolveFirstNavigableRoutePath(route)).toBe('/system/role')
  })

  it('redirect 指向隐藏子路由时被忽略，改走第一个可见子路由', () => {
    const route: NavigableRouteLike = {
      path: '/system',
      redirect: '/system/secret',
      children: [{ path: 'secret', meta: { hidden: true } }, { path: 'user' }],
    }

    expect(resolveFirstNavigableRoutePath(route)).toBe('/system/user')
  })

  it('redirect 指向本层之外的路径时被忽略，避免跳出当前菜单树', () => {
    const route: NavigableRouteLike = {
      path: '/system',
      redirect: '/other',
      children: [{ path: 'user' }],
    }

    expect(resolveFirstNavigableRoutePath(route)).toBe('/system/user')
  })

  it('redirect 等于自身路径时被忽略，防止原地打转', () => {
    const route: NavigableRouteLike = { path: '/system', redirect: '/system' }
    expect(resolveFirstNavigableRoutePath(route)).toBe('/system')
  })

  it('redirect 为函数等非字符串时被忽略', () => {
    const route: NavigableRouteLike = {
      path: '/system',
      redirect: () => '/system/user',
      children: [{ path: 'user' }],
    }

    expect(resolveFirstNavigableRoutePath(route)).toBe('/system/user')
  })

  it('传入父路径时相对路径按父路径拼接', () => {
    expect(resolveFirstNavigableRoutePath({ path: 'user' }, '/system')).toBe('/system/user')
  })

  it('children 为空数组时等同于叶子节点', () => {
    expect(resolveFirstNavigableRoutePath({ path: '/system', children: [] })).toBe('/system')
  })
})

describe('resolveFirstNavigableRouteListPath', () => {
  it('取第一个可见路由解析出的路径', () => {
    const routes: NavigableRouteLike[] = [
      { path: '/workbench', children: [{ path: 'dashboard' }] },
      { path: '/system' },
    ]

    expect(resolveFirstNavigableRouteListPath(routes)).toBe('/workbench/dashboard')
  })

  it('隐藏的顶层路由不作为结果，但仍下钻它的子路由', () => {
    const routes: NavigableRouteLike[] = [
      { path: '/layout', meta: { hidden: true }, children: [{ path: 'home' }] },
      { path: '/system' },
    ]

    expect(resolveFirstNavigableRouteListPath(routes)).toBe('/layout/home')
  })

  it('隐藏顶层路由没有可用子路由时继续看下一个顶层路由', () => {
    const routes: NavigableRouteLike[] = [
      { path: '/empty', meta: { hidden: true } },
      { path: '/system' },
    ]

    expect(resolveFirstNavigableRouteListPath(routes)).toBe('/system')
  })

  it('未定义与空列表都返回空串，调用方据此判断无可导航项', () => {
    expect(resolveFirstNavigableRouteListPath(undefined)).toBe('')
    expect(resolveFirstNavigableRouteListPath([])).toBe('')
  })

  it('全部顶层路由都隐藏且无子路由时返回空串', () => {
    const routes: NavigableRouteLike[] = [
      { path: '/a', meta: { hidden: true } },
      { path: '/b', meta: { hidden: true } },
    ]

    expect(resolveFirstNavigableRouteListPath(routes)).toBe('')
  })

  it('传入父路径时顶层相对路径按其拼接', () => {
    expect(resolveFirstNavigableRouteListPath([{ path: 'user' }], '/system')).toBe('/system/user')
  })

  it('顶层路由 path 为空时解析成根路径并直接返回', () => {
    expect(resolveFirstNavigableRouteListPath([{ path: '' }])).toBe('/')
  })
})
