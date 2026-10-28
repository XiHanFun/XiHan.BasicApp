import type { PermissionGrantItem } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, ref } from 'vue'
import { permissions as permissionsApi, userGrants as userGrantsApi } from '~/contracts'
import { ApiError } from '~/request'

export type GrantAction = ResponseOf<'user-grants.list'>['grants'][number]['action']
type CatalogItem = ResponseOf<'permissions.list'>['items'][number]
type UserRow = ResponseOf<'users.list'>['items'][number]

/** 穿梭框条目：权限码即主键；当前有、本侧目录里没有的码（如超管的 `*`）也列出来，标 outside，免得一动穿梭框就被悄悄撤掉 */
export interface UserGrantItem extends PermissionGrantItem {
  basicId: string
  outside: boolean
}

/**
 * 某个动作的右栏变成 next：移出本栏的回到未设置，移入本栏的落为该动作。授予与拒绝互斥——原在另一栏的权限挪进来，就从另一栏移走（与 4.x 相同）。
 */
export function applyPermissionTransfer(actions: ReadonlyMap<string, GrantAction>, action: GrantAction, next: readonly string[]): Map<string, GrantAction> {
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
 * 直接授权的差量：新增或改了动作的分别交给授予 / 拒绝，草稿里已没有的当前项撤销；动作没变的不重复提交。
 */
export function diffPermissionGrants(actions: ReadonlyMap<string, GrantAction>, current: ReadonlyMap<string, GrantAction>): { grant: string[], deny: string[], revoke: string[] } {
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
  const revoke = [...current.keys()].filter(code => !actions.has(code))
  return { grant, deny, revoke }
}

/** 目录 + 当前直接授权 → 穿梭框条目，按「包.资源」分段 */
export function toGrantItems(catalog: readonly CatalogItem[], current: Iterable<string>, outsideGroup: string): UserGrantItem[] {
  const items: UserGrantItem[] = catalog.map(item => ({
    basicId: item.code,
    permissionCode: item.code,
    permissionName: item.displayName,
    groupCode: `${item.package}.${item.resource}`,
    groupName: `${item.package}.${item.resource}`,
    outside: false,
  }))
  const known = new Set(catalog.map(item => item.code))
  for (const code of current) {
    if (!known.has(code))
      items.push({ basicId: code, permissionCode: code, permissionName: code, groupCode: outsideGroup, groupName: outsideGroup, outside: true })
  }
  return items
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

/** 批量结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { permission: string, code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => `${f.permission}: ${f.code}`).join('; '))
}

/**
 * 用户直接授权抽屉（4.x 用户页的「直接授权」）：授予与拒绝两个页签共用一份草稿，保存时按差量调 `user-grants.grant / deny / revoke`。
 */
export function useUserGrants(outsideGroup: () => string) {
  const open = ref(false)
  const user = ref<UserRow | null>(null)
  const tab = ref<'deny' | 'grant'>('grant')
  const loading = ref(false)
  const error = ref<null | string>(null)
  const catalog = ref<CatalogItem[]>([])
  const current = ref(new Map<string, GrantAction>())
  const actions = ref(new Map<string, GrantAction>())
  const dirty = ref(false)

  const items = computed(() => toGrantItems(catalog.value, current.value.keys(), outsideGroup()))
  const grantIds = computed(() => [...actions.value].filter(([, action]) => action === 'Grant').map(([code]) => code))
  const denyIds = computed(() => [...actions.value].filter(([, action]) => action === 'Deny').map(([code]) => code))

  async function reloadGrants(userId: string) {
    const { grants } = await userGrantsApi.list({ userId })
    current.value = new Map(grants.map(grant => [grant.permission, grant.action] as const))
    actions.value = new Map(current.value)
    dirty.value = false
  }

  async function start(row: UserRow) {
    user.value = row
    open.value = true
    tab.value = 'grant'
    error.value = null
    current.value = new Map()
    actions.value = new Map()
    loading.value = true
    try {
      const [, list] = await Promise.all([reloadGrants(row.id), catalog.value.length > 0 ? Promise.resolve(null) : permissionsApi.list({})])
      if (list)
        catalog.value = [...list.items]
    }
    catch (e) {
      error.value = describe(e)
    }
    finally {
      loading.value = false
    }
  }

  function transfer(action: GrantAction, next: readonly string[]) {
    if (loading.value)
      return
    actions.value = applyPermissionTransfer(actions.value, action, next)
    dirty.value = true
  }

  /** 保存：返回各动作提交了几项；没有变化返回 null */
  async function save(): Promise<{ grant: number, deny: number, revoke: number } | null> {
    const target = user.value
    if (!target || loading.value)
      return null
    const diff = diffPermissionGrants(actions.value, current.value)
    if (diff.grant.length === 0 && diff.deny.length === 0 && diff.revoke.length === 0) {
      dirty.value = false
      return null
    }
    loading.value = true
    try {
      if (diff.revoke.length > 0)
        failedCodes(await userGrantsApi.revoke({ userId: target.id, permissions: diff.revoke }))
      if (diff.grant.length > 0)
        failedCodes(await userGrantsApi.grant({ userId: target.id, permissions: diff.grant }))
      if (diff.deny.length > 0)
        failedCodes(await userGrantsApi.deny({ userId: target.id, permissions: diff.deny }))
      await reloadGrants(target.id)
      return { grant: diff.grant.length, deny: diff.deny.length, revoke: diff.revoke.length }
    }
    finally {
      loading.value = false
    }
  }

  return { open, user, tab, loading, error, items, actions, grantIds, denyIds, dirty, start, transfer, save }
}
