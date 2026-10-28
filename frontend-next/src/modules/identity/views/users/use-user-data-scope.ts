import type { DepartmentNode, MemberDataScopeDraft } from '@/modules/access/views/roles/data-scope'
import type { ResponseOf } from '~/contracts'
import { computed, ref } from 'vue'
import { departments as departmentsApi, userDataScopes as userDataScopesApi } from '~/contracts'
import { ApiError } from '~/request'

type UserRow = ResponseOf<'users.list'>['items'][number]

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function same(left: MemberDataScopeDraft, right: MemberDataScopeDraft): boolean {
  return left.dataScope === right.dataScope
    && left.departments.length === right.departments.length
    && left.departments.every(d => right.departments.some(o => o.departmentId === d.departmentId && o.includeChildren === d.includeChildren))
}

/**
 * 成员数据范围抽屉（4.x 用户页的「数据范围」）：「跟随角色」即不覆盖；覆盖完全取代角色的数据范围。只在租户侧。
 */
export function useUserDataScope() {
  const open = ref(false)
  const user = ref<UserRow | null>(null)
  const loading = ref(false)
  const error = ref<null | string>(null)
  const tree = ref<DepartmentNode[]>([])
  const draft = ref<MemberDataScopeDraft>({ dataScope: null, departments: [] })
  const original = ref<MemberDataScopeDraft>({ dataScope: null, departments: [] })

  const dirty = computed(() => !same(draft.value, original.value))
  /** 自定义要至少一个部门才能保存 */
  const complete = computed(() => draft.value.dataScope !== 'Custom' || draft.value.departments.length > 0)

  async function start(row: UserRow) {
    user.value = row
    open.value = true
    error.value = null
    draft.value = { dataScope: null, departments: [] }
    original.value = { dataScope: null, departments: [] }
    loading.value = true
    try {
      const [setting, departments] = await Promise.all([userDataScopesApi.get({ userId: row.id }), tree.value.length > 0 ? Promise.resolve(null) : departmentsApi.tree({})])
      if (departments)
        tree.value = [...departments.items]
      const loaded: MemberDataScopeDraft = { dataScope: setting.dataScope ?? null, departments: setting.departments.map(d => ({ departmentId: d.departmentId, includeChildren: d.includeChildren ?? false })) }
      draft.value = loaded
      original.value = { ...loaded, departments: [...loaded.departments] }
    }
    catch (e) {
      error.value = describe(e)
    }
    finally {
      loading.value = false
    }
  }

  async function save(): Promise<boolean> {
    const target = user.value
    if (!target || loading.value || !complete.value)
      return false
    loading.value = true
    try {
      await userDataScopesApi.set({ userId: target.id, dataScope: draft.value.dataScope, departments: draft.value.dataScope === 'Custom' ? draft.value.departments : [] })
      original.value = { ...draft.value, departments: [...draft.value.departments] }
      return true
    }
    finally {
      loading.value = false
    }
  }

  return { open, user, loading, error, tree, draft, dirty, complete, start, save }
}
