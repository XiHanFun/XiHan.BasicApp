import type { UserRow } from './use-users'
import type { ResponseOf } from '~/contracts'
import { ref } from 'vue'
import { memberships as membershipsApi, roles as rolesApi, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'
import { getDisplayTimeZone } from '~/utils'

export type UserRole = ResponseOf<'roles.by-user'>['items'][number]
export type MembershipRow = ResponseOf<'memberships.list'>['items'][number]
export type UserDetail = ResponseOf<'users.detail'>

/** 详情里要装哪些区块（由页面按权限码与上下文决定，composable 不碰授权） */
export interface DetailSections {
  roles: boolean
  departments: boolean
}

/** 一个用户的部门归属（按排序号与加入时间） */
export async function loadMemberships(userId: string): Promise<MembershipRow[]> {
  return (await membershipsApi.list({
    page: { index: 1, size: 100 },
    filters: [
      { field: 'userId', operator: 'eq', value: userId },
    ],
    sorts: [
      { field: 'order' },
      { field: 'createdAt' },
    ],
  })).items
}

/**
 * 查看详情：账号资料先用列表行顶上，账号安全、今日统计与当前会话取 users.detail（只要读用户的权限）；
 * 角色与部门归属按区块另取，区块没开的保持 null（页面退回列表行的角色与主部门）；取数失败把错误文案留在 error 上
 */
export function useUserDetail() {
  const open = ref(false)
  const user = ref<null | UserRow>(null)
  const detail = ref<null | UserDetail>(null)
  const sections = ref<DetailSections>({ roles: false, departments: false })
  const roles = ref<null | UserRole[]>(null)
  const memberships = ref<MembershipRow[] | null>(null)
  const loading = ref(false)
  const error = ref<null | string>(null)
  // 连点两行时只认最后一次：慢响应回来不覆盖新选中的用户
  let seq = 0

  async function show(row: UserRow, wanted: DetailSections): Promise<void> {
    const current = ++seq
    user.value = row
    detail.value = null
    sections.value = wanted
    roles.value = null
    memberships.value = null
    error.value = null
    open.value = true
    loading.value = true
    try {
      const [overview, roleList, membershipList] = await Promise.all([
        usersApi.detail({
          id: row.id,
          timeZone: getDisplayTimeZone(),
        }),
        wanted.roles
          ? rolesApi.byUser({ userId: row.id }).then(result => result.items)
          : Promise.resolve(null),
        wanted.departments
          ? loadMemberships(row.id)
          : Promise.resolve(null),
      ])
      if (current !== seq)
        return
      detail.value = overview
      roles.value = roleList
      memberships.value = membershipList
    }
    catch (e) {
      if (current === seq) {
        error.value = e instanceof ApiError
          ? e.detail
          : String(e)
      }
    }
    finally {
      if (current === seq)
        loading.value = false
    }
  }

  return {
    open,
    user,
    detail,
    sections,
    roles,
    memberships,
    loading,
    error,
    show,
  }
}
