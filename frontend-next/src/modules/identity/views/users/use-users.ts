import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { toPageRequest, toSchemaPageResult, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'

export type UserRow = ResponseOf<'users.list'>['items'][number]
export type UserStatus = UserRow['status']

export const USER_STATUSES: readonly UserStatus[] = ['Active', 'Locked', 'Disabled']

/** 弹窗表单：新建与编辑共用一份（用户名只在新建时可填，初始密码只在新建时出现） */
export interface UserForm {
  userName: string
  displayName: string
  email: string
  phoneNumber: string
  password: string
}

export type UserEditorMode = 'create' | 'edit'

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

function emptyForm(): UserForm {
  return { userName: '', displayName: '', email: '', phoneNumber: '', password: '' }
}

/** 批量操作的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/** 展示名：显示名为空时退回用户名 */
export function displayNameOf(row: Pick<UserRow, 'displayName' | 'userName'>): string {
  return row.displayName || row.userName
}

/**
 * SchemaPage 的资源：分页与删除（批量删除由 SchemaPage 逐行调用）。
 * 搜索字段按后端白名单下发：关键字匹配用户名与显示名，邮箱模糊匹配，状态精确匹配。
 * 6.0 的用户状态只能经锁定 / 解锁改，没有启停接口，所以不提供 updateStatus；建用户可带初始密码、导入没有安全的映射，所以不提供 create。
 */
export function usersResource(): SchemaResource<UserRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await usersApi.list(toPageRequest(params, { filters: { email: 'contains', status: 'eq' } })),
      params,
    ),
    remove: async id => failedCodes(await usersApi.delete({ ids: [String(id)] })),
  }
}

/** 锁定（状态置为 Locked、会话下线）或解锁一个用户；被后端逐项拒绝时带着错误码抛出 */
export async function setUserLocked(id: string, locked: boolean): Promise<void> {
  failedCodes(locked ? await usersApi.lock({ ids: [id] }) : await usersApi.unlock({ ids: [id] }))
}

/** 重置一个用户的双因素（清掉全部方式与认证器密钥，下次只凭密码登录）；被后端逐项拒绝时带着错误码抛出 */
export async function resetUserTwoFactor(id: string): Promise<void> {
  failedCodes(await usersApi.resetTwoFactor({ ids: [id] }))
}

/** 生成临时密码：大小写 + 数字 + 符号各至少一个，规避易混淆字符（0O1lI），满足后端「至少 8 位、含字母与数字」的策略 */
export function generateTempPassword(length = 12): string {
  const upper = 'ABCDEFGHJKMNPQRSTUVWXYZ'
  const lower = 'abcdefghjkmnpqrstuvwxyz'
  const digits = '23456789'
  const symbols = '!@#$%&*'
  const all = upper + lower + digits + symbols
  const buf = new Uint32Array(length)
  crypto.getRandomValues(buf)
  const pick = (set: string, seed: number) => set[seed % set.length]!
  const chars = [pick(upper, buf[0]!), pick(lower, buf[1]!), pick(digits, buf[2]!), pick(symbols, buf[3]!)]
  for (let i = chars.length; i < length; i++)
    chars.push(pick(all, buf[i]!))
  // Fisher-Yates 打乱（复用随机源）
  for (let i = chars.length - 1; i > 0; i--) {
    const j = buf[i]! % (i + 1)
    ;[chars[i], chars[j]] = [chars[j]!, chars[i]!]
  }
  return chars.join('')
}

/** ApiError 的字段级错误挂回字段，其余错误给整条提示 */
function captureErrors(error: unknown, fieldErrors: { value: Record<string, string[]> }, submitError: { value: null | string }) {
  if (error instanceof ApiError) {
    fieldErrors.value = error.errors ?? {}
    if (!error.hasFieldErrors)
      submitError.value = error.detail
  }
  else {
    submitError.value = String(error)
  }
}

/**
 * 用户页的编辑弹窗：新建（用户名、显示名、邮箱、手机号与可选的初始密码）与编辑资料（显示名、邮箱、手机号）。
 * 建在调用方当前所在的一侧（平台主体建平台账号，租户主体建本租户用户）。字段级错误挂回对应字段。
 */
export function useUserEditor() {
  const open = ref(false)
  const mode = ref<UserEditorMode>('create')
  const editing = ref<UserRow | null>(null)
  const form = reactive<UserForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')

  function reset(next: Partial<UserForm> = {}) {
    Object.assign(form, emptyForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate() {
    mode.value = 'create'
    editing.value = null
    reset()
    open.value = true
  }

  function startEdit(row: UserRow) {
    mode.value = 'edit'
    editing.value = row
    reset({ userName: row.userName, displayName: row.displayName, email: row.email ?? '', phoneNumber: row.phoneNumber ?? '' })
    open.value = true
  }

  function fieldError(field: keyof UserForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      if (mode.value === 'edit' && current) {
        await usersApi.update({
          id: current.id,
          displayName: form.displayName.trim(),
          email: blankToNull(form.email),
          phoneNumber: blankToNull(form.phoneNumber),
        })
      }
      else {
        await usersApi.create({
          userName: form.userName.trim(),
          displayName: form.displayName.trim(),
          email: blankToNull(form.email),
          phoneNumber: blankToNull(form.phoneNumber),
          // 不填即账号在管理员重置密码前不能登录（后端语义）
          password: form.password === '' ? null : form.password,
        })
      }
      open.value = false
      return true
    }
    catch (error) {
      captureErrors(error, fieldErrors, submitError)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, mode, isEdit, editing, form, fieldError, submitError, submitting, startCreate, startEdit, submit }
}

/**
 * 重置密码弹窗：打开时预填一份随机临时密码（可改、可重新生成），提交后用户下次登录须改密、全部会话下线。
 * 成功返回设下的密码，页面据此出一次性的复制框；字段级错误挂回字段。
 */
export function usePasswordReset() {
  const open = ref(false)
  const target = ref<UserRow | null>(null)
  const form = reactive({ newPassword: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  function start(row: UserRow) {
    target.value = row
    form.newPassword = generateTempPassword()
    fieldErrors.value = {}
    submitError.value = null
    open.value = true
  }

  function regenerate() {
    form.newPassword = generateTempPassword()
    fieldErrors.value = {}
  }

  function fieldError(field: 'newPassword'): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<null | string> {
    const current = target.value
    if (!current)
      return null
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const password = form.newPassword
      await usersApi.resetPassword({ id: current.id, newPassword: password })
      open.value = false
      return password
    }
    catch (error) {
      captureErrors(error, fieldErrors, submitError)
      return null
    }
    finally {
      submitting.value = false
    }
  }

  return { open, target, form, fieldError, submitError, submitting, start, regenerate, submit }
}
