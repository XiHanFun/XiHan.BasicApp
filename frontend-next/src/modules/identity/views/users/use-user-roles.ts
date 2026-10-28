import type { UserRow } from './use-users'
import type { GrantTransferItem } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, ref } from 'vue'
import { roles as rolesApi } from '~/contracts'

type RoleRow = ResponseOf<'roles.list'>['items'][number]

/** 穿梭框条目：主键即角色 ID */
export interface RoleGrantItem extends GrantTransferItem {
  basicId: string
  name: string
  code: string
  disabled: boolean
}

/** 一次保存的结果：新分配与移除的角色数 */
export interface RoleSaveResult {
  assigned: number
  unassigned: number
}

/** 后端 `PageInfo.MaxSize`：角色目录一次取全 */
const ROLE_CATALOG_SIZE = 500

function toItem(role: Pick<RoleRow, 'code' | 'id' | 'name' | 'status'>): RoleGrantItem {
  return { basicId: role.id, name: role.name, code: role.code, disabled: role.status === 'Disabled' }
}

/**
 * 分配角色抽屉：左栏是当前作用侧的角色目录、右栏是该用户已有的角色（`roles.by-user`），挪好后一次保存。
 * 保存按差异逐个角色 `roles.assign` / `roles.unassign`（6.0 的指派接口以角色为主体）；逐项被拒的错误码汇成一条抛出，
 * 已落地的部分随后重取，草稿与服务端对齐。用户须与角色同一侧（注册地 = 角色所属侧），页面据此决定入口出不出现。
 */
export function useUserRoles() {
  const open = ref(false)
  const user = ref<null | UserRow>(null)
  const loading = ref(false)
  const items = ref<RoleGrantItem[]>([])
  const assigned = ref<string[]>([])
  const draft = ref<string[]>([])

  const dirty = computed(() => {
    const current = new Set(assigned.value)
    return draft.value.length !== current.size || draft.value.some(id => !current.has(id))
  })

  async function load(userId: string) {
    const [catalog, mine] = await Promise.all([
      rolesApi.list({ page: { index: 1, size: ROLE_CATALOG_SIZE }, filters: [], sorts: [{ field: 'order' }, { field: 'code' }] }),
      rolesApi.byUser({ userId }),
    ])
    // 已有却不在目录里的（目录超过一页）补进条目，右栏「已分配」才看得全
    const known = new Set(catalog.items.map(role => role.id))
    items.value = [...catalog.items, ...mine.items.filter(role => !known.has(role.id))].map(toItem)
    assigned.value = mine.items.map(role => role.id)
    draft.value = [...assigned.value]
  }

  /** 打开抽屉并装载；取数失败时抛出（抽屉保持打开、条目为空），由页面提示 */
  async function start(row: UserRow): Promise<void> {
    user.value = row
    items.value = []
    assigned.value = []
    draft.value = []
    open.value = true
    loading.value = true
    try {
      await load(row.id)
    }
    finally {
      loading.value = false
    }
  }

  function change(next: string[]) {
    if (!loading.value)
      draft.value = next
  }

  async function save(): Promise<RoleSaveResult> {
    const current = user.value
    if (!current)
      return { assigned: 0, unassigned: 0 }
    const before = new Set(assigned.value)
    const after = new Set(draft.value)
    const toAssign = draft.value.filter(id => !before.has(id))
    const toUnassign = assigned.value.filter(id => !after.has(id))
    if (toAssign.length === 0 && toUnassign.length === 0)
      return { assigned: 0, unassigned: 0 }
    loading.value = true
    try {
      const failed: string[] = []
      let interrupted: unknown = null
      try {
        for (const roleId of toAssign)
          failed.push(...(await rolesApi.assign({ id: roleId, userIds: [current.id] })).failed.map(f => f.code))
        for (const roleId of toUnassign)
          failed.push(...(await rolesApi.unassign({ id: roleId, userIds: [current.id] })).failed.map(f => f.code))
      }
      catch (error) {
        interrupted = error
      }
      // 中途失败时前面几笔可能已落地：先按服务端重取，再把原因抛给页面
      await load(current.id)
      if (interrupted)
        throw interrupted
      if (failed.length > 0)
        throw new Error(failed.join('; '))
      return { assigned: toAssign.length, unassigned: toUnassign.length }
    }
    finally {
      loading.value = false
    }
  }

  return { open, user, loading, items, assigned, draft, dirty, start, change, save }
}
