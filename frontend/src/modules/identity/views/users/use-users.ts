/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ImportManyFailure, SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, RequestOf, ResponseOf } from '~/contracts'
import type { SelectOption } from '~/types'
import { computed, reactive, ref } from 'vue'
import { departments as departmentsApi, roles as rolesApi, sessions as sessionsApi, toPageRequest, toSchemaPageResult, users as usersApi } from '~/contracts'
import { ApiError, throwOnFailed } from '~/request'
import { flattenDepartments } from './use-user-departments'

export type UserRow = ResponseOf<'users.list'>['items'][number]
export type UserStatus = UserRow['status']
export type UserGender = UserRow['gender']

export const USER_STATUSES: readonly UserStatus[] = ['Active', 'Locked', 'Disabled']
export const USER_GENDERS: readonly UserGender[] = ['Unknown', 'Male', 'Female']

/** 弹窗表单：新建与编辑共用一份（用户名只在新建时可填，初始密码只在新建时出现） */
export interface UserForm {
  userName: string
  displayName: string
  email: string
  phoneNumber: string
  password: string
  realName: string
  gender: UserGender
  /** 日期选择器的值（毫秒时间戳），没填为空 */
  birthday: null | number
  country: string
  remark: string
}

export type UserEditorMode = 'create' | 'edit'

function blankToNull(value: string): null | string {
  return value.trim() === ''
    ? null
    : value.trim()
}

function emptyForm(): UserForm {
  return {
    userName: '',
    displayName: '',
    email: '',
    phoneNumber: '',
    password: '',
    realName: '',
    gender: 'Unknown',
    birthday: null,
    country: '',
    remark: '',
  }
}

/** 登录策略：只在编辑时读写，要有设置权限 */
export interface LoginPolicyForm {
  allowMultiLogin: boolean
  maxLoginDevices: number
}

/** 展示名：显示名为空时退回用户名 */
export function displayNameOf(row: Pick<UserRow, 'displayName' | 'userName'>): string {
  return row.displayName || row.userName
}

/**
 * 搜索字段按后端白名单下发：关键字匹配用户名与显示名，邮箱与手机号模糊匹配，性别、角色与部门精确匹配（部门含下级），
 * 状态多选，创建时间按区间
 */
export const USER_QUERY: PageQueryMapping = {
  filters: {
    email: 'contains',
    phoneNumber: 'contains',
    gender: 'eq',
    roleId: 'eq',
    departmentId: 'eq',
  },
  ranges: ['createdAt'],
  multiples: ['status'],
}

/** users.import 的一行：导入字段的 key 与行字段同名 */
export type UserImportRow = RequestOf<'users.import'>['rows'][number]

/** 一格里的多个编码：逗号、分号、顿号或空白分开（全角半角都认） */
export function splitCodes(value: unknown): string[] {
  return typeof value === 'string'
    ? [
        ...new Set(value.split(/[\s,;，；、]+/).filter(code => code !== '')),
      ]
    : []
}

/** 一格日期按本地日历日解析，与编辑弹窗的日期选择器同一口径（本地零点） */
export function localDateIso(text: string): string {
  const parts = /^(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})$/.exec(text.trim())
  return parts
    ? new Date(Number(parts[1]), Number(parts[2]) - 1, Number(parts[3])).toISOString()
    : new Date(text).toISOString()
}

/** 导入记录（导入字段 key → 解析后的值，没填的不在记录里）→ users.import 的一行 */
export function toUserImportRow(record: Record<string, unknown>): UserImportRow {
  const text = (key: string) => typeof record[key] === 'string' && record[key] !== ''
    ? record[key]
    : null
  const birthday = text('birthday')
  return {
    userName: text('userName'),
    displayName: text('displayName'),
    email: text('email'),
    phoneNumber: text('phoneNumber'),
    realName: text('realName'),
    gender: (record.gender as UserGender | undefined) ?? null,
    birthday: birthday === null
      ? null
      : localDateIso(birthday),
    country: text('country'),
    remark: text('remark'),
    departmentCode: text('departmentCode'),
    roleCodes: splitCodes(record.roleCodes),
  }
}

/**
 * 交一批导入记录给 users.import，返回不能导入的记录
 * 后端逐行按新建用户的规则校验，部门与角色在同一事务里写好；整批被拒（缺少入部门或配角色的权限、密码不合策略）时抛出
 */
export async function importUsers(records: Record<string, unknown>[], password: string): Promise<ImportManyFailure[]> {
  const result = await usersApi.import({
    rows: records.map(toUserImportRow),
    password,
  })
  return result.failed.map(failure => ({
    index: failure.index,
    field: failure.field ?? undefined,
    message: failure.message,
  }))
}

/**
 * SchemaPage 的资源：分页、删除、启停、批量导入与提交到导出中心
 * 导出时后台以发起人身份逐页调 users.list，查询快照与列表共用分页请求
 * 不提供逐条的 create：导入走 users.import，这批用户共用导入弹窗里填的初始密码
 */
export function usersResource(importPassword: () => string): SchemaResource<UserRow> {
  const toRequest = (params: SchemaQueryParams) => toPageRequest(params, USER_QUERY)
  return {
    importMany: records => importUsers(records, importPassword()),
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await usersApi.list(toRequest(params)), params),
    remove: async id => throwOnFailed(await usersApi.delete({
      ids: [
        String(id),
      ],
    })),
    updateStatus: async (id, enabled) => throwOnFailed(enabled
      ? await usersApi.enable({
          ids: [
            String(id),
          ],
        })
      : await usersApi.disable({
          ids: [
            String(id),
          ],
        })),
    export: { businessType: 'users.list', buildQuery: toRequest },
  }
}

/** 锁定（会话下线）或解锁一个用户，被拒时带着原因抛出 */
export async function setUserLocked(id: string, locked: boolean): Promise<void> {
  throwOnFailed(locked
    ? await usersApi.lock({
        ids: [id],
      })
    : await usersApi.unlock({
        ids: [id],
      }))
}

/**
 * 按人强制下线，返回下线的会话数
 * 取此人在当前一侧的有效会话交给 sessions.revoke，与在线用户页同一套语义与审计
 * 全部被拒时带着原因抛出
 */
export async function forceSignOut(userId: string, reason: string): Promise<number> {
  const page = await sessionsApi.list({
    page: { index: 1, size: 100 },
    filters: [
      { field: 'userId', operator: 'eq', value: userId },
    ],
    sorts: [],
  })
  const ids = page.items.map(session => session.id)
  if (ids.length === 0)
    return 0
  const result = await sessionsApi.revoke({ ids, reason })
  if (result.succeeded.length === 0)
    throwOnFailed(result)
  return result.succeeded.length
}

/** 角色筛选的选项：当前一侧的全部角色 */
export async function loadRoleOptions(): Promise<{ label: string, value: string }[]> {
  const page = await rolesApi.list({
    page: { index: 1, size: 500 },
    filters: [],
    sorts: [],
  })
  return page.items.map(role => ({ label: role.name, value: role.id }))
}

/** 部门筛选的选项：部门树按层级缩进展平（选上级即含下级） */
export async function loadDepartmentOptions(): Promise<SelectOption[]> {
  return flattenDepartments((await departmentsApi.tree({})).items)
}

/** 停用（会话下线、不能登录）或启用一个用户，被拒时带着原因抛出 */
export async function setUserDisabled(id: string, disabled: boolean): Promise<void> {
  throwOnFailed(disabled
    ? await usersApi.disable({
        ids: [id],
      })
    : await usersApi.enable({
        ids: [id],
      }))
}

/**
 * 重置双因素，被拒时带着原因抛出
 * 清掉全部方式与认证器密钥，下次只凭密码登录
 */
export async function resetUserTwoFactor(id: string): Promise<void> {
  throwOnFailed(await usersApi.resetTwoFactor({
    ids: [id],
  }))
}

/**
 * 生成临时密码：大小写、数字、符号各至少一个，不含易混淆字符（0O1lI）
 * 须满足后端「至少 8 位、含字母与数字」的策略
 */
export function generateTempPassword(length = 12): string {
  const upper = 'ABCDEFGHJKMNPQRSTUVWXYZ'
  const lower = 'abcdefghjkmnpqrstuvwxyz'
  const digits = '23456789'
  const symbols = '!@#$%&*'
  const all = upper + lower + digits + symbols
  const buf = new Uint32Array(length)
  crypto.getRandomValues(buf)
  const pick = (set: string, seed: number) => set[seed % set.length]!
  const chars = [
    pick(upper, buf[0]!),
    pick(lower, buf[1]!),
    pick(digits, buf[2]!),
    pick(symbols, buf[3]!),
  ]
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
 * 用户的新建与编辑弹窗，新建可带初始密码
 * 建在调用方当前所在的一侧（平台主体建平台账号，租户主体建本租户用户）
 * 外部成员由其注册地维护：打开即全部只读，提交不发请求，也不读登录策略
 */
export function useUserEditor(canSetLoginPolicy: () => boolean = () => false) {
  const open = ref(false)
  const mode = ref<UserEditorMode>('create')
  const editing = ref<UserRow | null>(null)
  const form = reactive<UserForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')
  const readonly = computed(() => isEdit.value && editing.value?.isExternalMember === true)
  const loginPolicy = reactive<LoginPolicyForm>({ allowMultiLogin: true, maxLoginDevices: 0 })
  const loadedPolicy = ref<LoginPolicyForm | null>(null)

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

  async function loadLoginPolicy(row: UserRow) {
    loadedPolicy.value = null
    if (!canSetLoginPolicy() || row.isExternalMember)
      return
    const policy = await usersApi.loginPolicy({ id: row.id })
    if (editing.value?.id !== row.id)
      return
    Object.assign(loginPolicy, { allowMultiLogin: policy.allowMultiLogin, maxLoginDevices: policy.maxLoginDevices })
    loadedPolicy.value = { allowMultiLogin: policy.allowMultiLogin, maxLoginDevices: policy.maxLoginDevices }
  }

  function startEdit(row: UserRow) {
    mode.value = 'edit'
    editing.value = row
    void loadLoginPolicy(row)
    reset({
      userName: row.userName,
      displayName: row.displayName,
      email: row.email ?? '',
      phoneNumber: row.phoneNumber ?? '',
      realName: row.realName ?? '',
      gender: row.gender,
      birthday: row.birthday
        ? new Date(row.birthday).getTime()
        : null,
      country: row.country ?? '',
      remark: row.remark ?? '',
    })
    open.value = true
  }

  function fieldError(field: keyof UserForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    if (readonly.value)
      return false
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      const birthday = form.birthday
        ? new Date(form.birthday).toISOString()
        : null
      if (mode.value === 'edit' && current) {
        await usersApi.update({
          id: current.id,
          displayName: form.displayName.trim(),
          email: blankToNull(form.email),
          phoneNumber: blankToNull(form.phoneNumber),
          realName: blankToNull(form.realName),
          gender: form.gender,
          birthday,
          country: blankToNull(form.country),
          remark: blankToNull(form.remark),
        })
        const before = loadedPolicy.value
        if (before && (before.allowMultiLogin !== loginPolicy.allowMultiLogin || before.maxLoginDevices !== loginPolicy.maxLoginDevices)) {
          await usersApi.setLoginPolicy({ id: current.id, allowMultiLogin: loginPolicy.allowMultiLogin, maxLoginDevices: loginPolicy.maxLoginDevices })
        }
      }
      else {
        await usersApi.create({
          userName: form.userName.trim(),
          displayName: form.displayName.trim(),
          email: blankToNull(form.email),
          phoneNumber: blankToNull(form.phoneNumber),
          // 不填即账号在管理员重置密码前不能登录（后端语义）
          password: form.password === ''
            ? null
            : form.password,
          realName: blankToNull(form.realName),
          // 未设置不传：字段安全把性别设成只读时，不填才能建
          gender: form.gender === 'Unknown'
            ? null
            : form.gender,
          birthday,
          country: blankToNull(form.country),
          remark: blankToNull(form.remark),
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

  return {
    open,
    mode,
    isEdit,
    readonly,
    editing,
    form,
    loginPolicy,
    loadedPolicy,
    fieldError,
    submitError,
    submitting,
    startCreate,
    startEdit,
    submit,
  }
}

/**
 * 重置密码弹窗，预填可改、可重新生成的随机临时密码
 * 提交后用户下次登录须改密、全部会话下线；成功返回设下的密码，供一次性复制
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

  return {
    open,
    target,
    form,
    fieldError,
    submitError,
    submitting,
    start,
    regenerate,
    submit,
  }
}
