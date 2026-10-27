import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { permissions as permissionsApi, roles as rolesApi, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'

export type RoleRow = ResponseOf<'roles.list'>['items'][number]
export type RoleDetail = ResponseOf<'roles.get'>
export type PermissionItem = ResponseOf<'permissions.list'>['items'][number]
export type RoleMember = ResponseOf<'roles.members'>['items'][number]
export type UserOption = ResponseOf<'users.list'>['items'][number]

export interface CreateRoleForm {
  code: string
  name: string
  description: string
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

/**
 * 角色页的状态与动作：分页列表、创建表单、选中角色的详情（授予草稿按包分组、保存时按差异授予 / 撤销）、成员增删、删除。
 * 契约类型来自生成物；错误只看 `code`、`detail` 与字段级 `errors`。
 */
export function useRoles(pageSize = 20) {
  const items = ref<RoleRow[]>([])
  const total = ref(0)
  const pageIndex = ref(1)
  const loading = ref(false)
  const listError = ref<string | null>(null)
  const pageCount = computed(() => Math.max(1, Math.ceil(total.value / pageSize)))

  const form = reactive<CreateRoleForm>({ code: '', name: '', description: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const catalog = ref<PermissionItem[]>([])
  const selected = ref<RoleDetail | null>(null)
  const draft = ref<Set<string>>(new Set())
  const members = ref<RoleMember[]>([])
  const userOptions = ref<UserOption[]>([])
  const detailError = ref<string | null>(null)
  const notice = ref<string | null>(null)
  const working = ref(false)

  /** 目录按包分组，供复选框渲染 */
  const groups = computed(() => {
    const byPackage = new Map<string, PermissionItem[]>()
    for (const item of catalog.value)
      byPackage.set(item.package, [...(byPackage.get(item.package) ?? []), item])
    return [...byPackage.entries()].map(([pkg, list]) => ({ package: pkg, items: list }))
  })
  const dirty = computed(() => {
    const current = new Set(selected.value?.permissions ?? [])
    return current.size !== draft.value.size || [...draft.value].some(code => !current.has(code))
  })

  async function load(index = pageIndex.value) {
    loading.value = true
    listError.value = null
    try {
      const result = await rolesApi.list({ page: { index, size: pageSize }, filters: [], sorts: [{ field: 'order' }, { field: 'code' }] })
      items.value = result.items
      total.value = Number(result.total)
      pageIndex.value = result.page.index ?? index
    }
    catch (error) {
      items.value = []
      total.value = 0
      listError.value = describe(error)
    }
    finally {
      loading.value = false
    }
  }

  function fieldError(field: keyof CreateRoleForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const created = await rolesApi.create({ code: form.code.trim(), name: form.name.trim(), description: form.description.trim() || null })
      form.code = ''
      form.name = ''
      form.description = ''
      await load(1)
      await select(created.id)
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        fieldErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          submitError.value = error.detail
      }
      else {
        submitError.value = String(error)
      }
      return false
    }
    finally {
      submitting.value = false
    }
  }

  async function loadCatalog() {
    if (catalog.value.length === 0)
      catalog.value = (await permissionsApi.list({})).items
  }

  async function loadMembers(id: string) {
    members.value = (await rolesApi.members({ id })).items
  }

  /** 候选用户（本侧前 100 个）：只有能读用户的主体才会调用 */
  async function loadUserOptions() {
    const result = await usersApi.list({ page: { index: 1, size: 100 }, filters: [], sorts: [{ field: 'userName' }] })
    userOptions.value = result.items
  }

  async function select(id: string) {
    detailError.value = null
    notice.value = null
    try {
      const detail = await rolesApi.get({ id })
      selected.value = detail
      draft.value = new Set(detail.permissions)
      await Promise.all([loadCatalog(), loadMembers(id)])
    }
    catch (error) {
      selected.value = null
      detailError.value = describe(error)
    }
  }

  function togglePermission(code: string) {
    const next = new Set(draft.value)
    if (next.has(code))
      next.delete(code)
    else next.add(code)
    draft.value = next
  }

  /** 按差异提交：新勾的授予、取消的撤销；逐项失败以错误码显示 */
  async function savePermissions(): Promise<boolean> {
    const role = selected.value
    if (!role)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const current = new Set(role.permissions)
      const toGrant = [...draft.value].filter(code => !current.has(code))
      const toRevoke = role.permissions.filter(code => !draft.value.has(code))
      const failed: string[] = []
      if (toGrant.length > 0)
        failed.push(...(await rolesApi.grant({ id: role.role.id, permissions: toGrant })).failed.map(f => `${f.permission}: ${f.code}`))
      if (toRevoke.length > 0)
        failed.push(...(await rolesApi.revoke({ id: role.role.id, permissions: toRevoke })).failed.map(f => `${f.permission}: ${f.code}`))
      await select(role.role.id)
      if (failed.length > 0) {
        detailError.value = failed.join('; ')
        return false
      }
      notice.value = 'saved'
      return true
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  async function addMember(userId: string): Promise<boolean> {
    const role = selected.value
    if (!role || !userId)
      return false
    working.value = true
    detailError.value = null
    try {
      const result = await rolesApi.assign({ id: role.role.id, userIds: [userId] })
      if (result.failed.length > 0)
        detailError.value = result.failed.map(f => f.code).join('; ')
      await select(role.role.id)
      return result.failed.length === 0
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  async function removeMember(userId: string): Promise<boolean> {
    const role = selected.value
    if (!role)
      return false
    working.value = true
    detailError.value = null
    try {
      await rolesApi.unassign({ id: role.role.id, userIds: [userId] })
      await select(role.role.id)
      return true
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  async function remove(): Promise<boolean> {
    const role = selected.value
    if (!role)
      return false
    working.value = true
    detailError.value = null
    try {
      const result = await rolesApi.delete({ ids: [role.role.id] })
      if (result.failed.length > 0) {
        detailError.value = result.failed.map(f => f.code).join('; ')
        return false
      }
      selected.value = null
      members.value = []
      await load(1)
      return true
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  return {
    items,
    total,
    pageIndex,
    pageCount,
    pageSize,
    loading,
    listError,
    form,
    fieldErrors,
    fieldError,
    submitError,
    submitting,
    catalog,
    groups,
    selected,
    draft,
    dirty,
    members,
    userOptions,
    detailError,
    notice,
    working,
    load,
    submit,
    select,
    loadUserOptions,
    togglePermission,
    savePermissions,
    addMember,
    removeMember,
    remove,
  }
}
