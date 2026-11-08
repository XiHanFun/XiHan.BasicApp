/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import type { SelectOption } from '~/types'
import { computed, reactive, ref } from 'vue'
import { aiAssistants, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError, throwOnFailed } from '~/request'

export type AssistantRow = ResponseOf<'ai-assistants.list'>['items'][number]
export type AssistantDetail = ResponseOf<'ai-assistants.get'>
export type AssistantStatus = AssistantRow['status']

/** 编辑弹窗的表单：新建与编辑共用 */
export interface AssistantForm {
  code: string
  name: string
  avatar: string
  description: string
  greeting: string
  promptCode: string
  providerCode: string
  enableKnowledge: boolean
  knowledgeProviderCode: string
  knowledgeTopK: number
  historyRounds: number
  tools: string[]
  isDefault: boolean
  order: number
  status: AssistantStatus
  remark: string
}

export function emptyAssistantForm(): AssistantForm {
  return {
    code: '',
    name: '',
    avatar: '',
    description: '',
    greeting: '',
    promptCode: '',
    providerCode: '',
    enableKnowledge: true,
    knowledgeProviderCode: '',
    knowledgeTopK: 5,
    historyRounds: 10,
    tools: [],
    isDefault: false,
    order: 100,
    status: 'Active',
    remark: '',
  }
}

function blankToNull(value: string): null | string {
  return value.trim() === ''
    ? null
    : value.trim()
}

/**
 * SchemaPage 的资源：分页与删除
 * 搜索字段按后端白名单下发：状态多选，关键字匹配编码、名称与简介
 */
export function assistantsResource(): SchemaResource<AssistantRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await aiAssistants.list(toPageRequest(params, {
        multiples: ['status'],
      })),
      params,
    ),
    remove: async id => throwOnFailed(await aiAssistants.delete({
      ids: [
        String(id),
      ],
    })),
  }
}

/**
 * 助手的编辑弹窗：新建与编辑（编码只在新建时填，状态只在新建时选，之后走行内启停）
 * 提供商与提示词填编码；可用的工具从后端目录勾选（方案 A3），没有向量库时给出提示（方案 D7b）
 */
export function useAssistantEditor() {
  const open = ref(false)
  const editing = ref<AssistantDetail | null>(null)
  const form = reactive<AssistantForm>(emptyAssistantForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)
  const isEdit = computed(() => editing.value !== null)
  const catalog = ref<ResponseOf<'ai-assistants.tools'>['items']>([])
  const knowledgeAvailable = ref(true)

  /** 目录里的工具，加上已勾选而目录里没有的（模块关掉了）：留着可见，保存时后端逐个拒绝 */
  const toolOptions = computed<SelectOption[]>(() => {
    const known = new Set(catalog.value.map(tool => tool.name))
    return [
      ...catalog.value.map(tool => ({
        value: tool.name,
        label: tool.description
          ? `${tool.name} - ${tool.description}`
          : tool.name,
      })),
      ...form.tools.filter(name => !known.has(name)).map(name => ({ value: name, label: name })),
    ]
  })

  function reset(next: Partial<AssistantForm> = {}) {
    Object.assign(form, emptyAssistantForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  /** 工具目录：每次打开弹窗都取，知识库能否使用也随之更新 */
  async function loadTools() {
    const result = await aiAssistants.tools({})
    catalog.value = result.items
    knowledgeAvailable.value = result.knowledgeAvailable
  }

  function startCreate() {
    editing.value = null
    reset()
    open.value = true
  }

  async function startEdit(id: string) {
    const detail = await aiAssistants.get({ id })
    editing.value = detail
    reset({
      code: detail.code,
      name: detail.name,
      avatar: detail.avatar ?? '',
      description: detail.description ?? '',
      greeting: detail.greeting ?? '',
      promptCode: detail.promptCode ?? '',
      providerCode: detail.providerCode ?? '',
      enableKnowledge: detail.enableKnowledge,
      knowledgeProviderCode: detail.knowledgeProviderCode ?? '',
      knowledgeTopK: detail.knowledgeTopK,
      historyRounds: detail.historyRounds,
      tools: [...detail.tools],
      isDefault: detail.isDefault,
      order: detail.order,
      status: detail.status,
      remark: detail.remark ?? '',
    })
    open.value = true
  }

  function fieldError(field: keyof AssistantForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      const common = {
        name: form.name.trim(),
        avatar: blankToNull(form.avatar),
        description: blankToNull(form.description),
        greeting: blankToNull(form.greeting),
        promptCode: blankToNull(form.promptCode),
        providerCode: blankToNull(form.providerCode),
        enableKnowledge: form.enableKnowledge,
        knowledgeProviderCode: blankToNull(form.knowledgeProviderCode),
        knowledgeTopK: form.knowledgeTopK,
        historyRounds: form.historyRounds,
        tools: form.tools,
        isDefault: form.isDefault,
        order: form.order,
        remark: blankToNull(form.remark),
      }
      if (current) {
        await aiAssistants.update({ id: current.id, concurrencyStamp: current.concurrencyStamp, ...common })
      }
      else {
        await aiAssistants.create({
          ...common,
          code: form.code.trim(),
          status: form.status,
        })
      }
      open.value = false
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

  return {
    open,
    editing,
    isEdit,
    form,
    fieldError,
    submitError,
    submitting,
    toolOptions,
    knowledgeAvailable,
    loadTools,
    startCreate,
    startEdit,
    submit,
  }
}
