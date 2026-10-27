import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { tenants as tenantsApi } from '~/contracts'
import { ApiError } from '~/request'

export type TenantRow = ResponseOf<'tenants.list'>['items'][number]
export type TenantDetail = ResponseOf<'tenants.get'>

export interface CreateTenantForm {
  tenantId: string
  name: string
  description: string
  order: string
}

export interface EditTenantForm {
  name: string
  description: string
  order: string
}

export interface InitializeAdminForm {
  userName: string
  password: string
  displayName: string
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function toOrder(value: string): number {
  return value.trim() === '' ? 0 : Number(value)
}

/**
 * 租户页的状态与动作（平台侧）：分页列表、开通表单、选中租户的详情与编辑、管理员初始化、停用 / 恢复、删除。
 * 契约类型来自生成物；错误只看 `code`、`detail` 与字段级 `errors`；批量操作逐项失败以错误码显示。
 */
export function useTenants(pageSize = 20) {
  const items = ref<TenantRow[]>([])
  const total = ref(0)
  const pageIndex = ref(1)
  const loading = ref(false)
  const listError = ref<string | null>(null)
  const pageCount = computed(() => Math.max(1, Math.ceil(total.value / pageSize)))

  const form = reactive<CreateTenantForm>({ tenantId: '', name: '', description: '', order: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const selected = ref<TenantDetail | null>(null)
  const edit = reactive<EditTenantForm>({ name: '', description: '', order: '' })
  const editErrors = ref<Record<string, string[]>>({})
  const adminForm = reactive<InitializeAdminForm>({ userName: '', password: '', displayName: '' })
  const adminErrors = ref<Record<string, string[]>>({})
  const detailError = ref<string | null>(null)
  const notice = ref<'saved' | 'admin_created' | null>(null)
  const working = ref(false)

  async function load(index = pageIndex.value) {
    loading.value = true
    listError.value = null
    try {
      const result = await tenantsApi.list({ page: { index, size: pageSize }, filters: [], sorts: [{ field: 'order' }, { field: 'tenantId' }] })
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

  function fieldError(field: keyof CreateTenantForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  function editError(field: keyof EditTenantForm): string | undefined {
    return editErrors.value[field]?.[0]
  }

  function adminError(field: keyof InitializeAdminForm): string | undefined {
    return adminErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const created = await tenantsApi.create({
        tenantId: form.tenantId.trim(),
        name: form.name.trim(),
        description: form.description.trim() || null,
        order: toOrder(form.order),
      })
      form.tenantId = ''
      form.name = ''
      form.description = ''
      form.order = ''
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

  async function select(id: string) {
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    adminErrors.value = {}
    try {
      const detail = await tenantsApi.get({ id })
      selected.value = detail
      edit.name = detail.tenant.name
      edit.description = detail.tenant.description ?? ''
      edit.order = String(detail.tenant.order)
      adminForm.userName = ''
      adminForm.password = ''
      adminForm.displayName = ''
    }
    catch (error) {
      selected.value = null
      detailError.value = describe(error)
    }
  }

  /** 编辑：名称、说明、排序；名称由后端同步到租户目录 */
  async function save(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    try {
      await tenantsApi.update({ id: current.tenant.id, name: edit.name.trim(), description: edit.description.trim() || null, order: toOrder(edit.order) })
      await Promise.all([load(), select(current.tenant.id)])
      notice.value = 'saved'
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        editErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          detailError.value = error.detail
      }
      else {
        detailError.value = String(error)
      }
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 初始化管理员：每个租户一次；成功后详情显示管理员登录名 */
  async function initializeAdmin(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    adminErrors.value = {}
    try {
      await tenantsApi.initializeAdmin({
        id: current.tenant.id,
        userName: adminForm.userName.trim(),
        password: adminForm.password,
        displayName: adminForm.displayName.trim() || null,
      })
      await Promise.all([load(), select(current.tenant.id)])
      notice.value = 'admin_created'
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        adminErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          detailError.value = error.detail
      }
      else {
        detailError.value = String(error)
      }
      return false
    }
    finally {
      working.value = false
    }
  }

  async function setStatus(action: 'suspend' | 'resume'): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await (action === 'suspend' ? tenantsApi.suspend : tenantsApi.resume)({ ids: [current.tenant.id] })
      if (result.failed.length > 0)
        detailError.value = result.failed.map(f => f.code).join('; ')
      await Promise.all([load(), select(current.tenant.id)])
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

  const suspend = () => setStatus('suspend')
  const resume = () => setStatus('resume')

  /** 删除：还有用户的租户由后端逐项拒绝（TENANCY_CONFLICT_003） */
  async function remove(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await tenantsApi.delete({ ids: [current.tenant.id] })
      if (result.failed.length > 0) {
        detailError.value = result.failed.map(f => f.code).join('; ')
        return false
      }
      selected.value = null
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
    selected,
    edit,
    editError,
    adminForm,
    adminError,
    detailError,
    notice,
    working,
    load,
    submit,
    select,
    save,
    initializeAdmin,
    suspend,
    resume,
    remove,
  }
}
