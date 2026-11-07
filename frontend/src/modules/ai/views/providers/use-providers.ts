import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { aiProviders, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError, throwOnFailed } from '~/request'

export type ProviderRow = ResponseOf<'ai-providers.list'>['items'][number]
export type ProviderDetail = ResponseOf<'ai-providers.get'>
export type ProviderStatus = ProviderRow['status']
export type ProviderTestResult = ResponseOf<'ai-providers.test'>

/** 厂商建议项的值（只用于分组展示，都走 OpenAI 兼容协议，可自填） */
export const VENDOR_VALUES = [
  'OpenAI',
  'DeepSeek',
  'Azure',
  'Moonshot',
  'SelfHosted',
  'Custom',
] as const

/** 品牌厂商的显示名；自建与自定义按语言取文案 */
const VENDOR_BRAND_LABELS: Record<string, string> = {
  OpenAI: 'OpenAI',
  DeepSeek: 'DeepSeek',
  Azure: 'Azure OpenAI',
  Moonshot: 'Moonshot',
}

/** 这个厂商要不要写端点：只有 OpenAI 能留空（校验在后端，这里只负责标必填） */
export function endpointRequired(vendor: string): boolean {
  const name = vendor.trim().toLowerCase()
  return name !== '' && name !== 'openai'
}

/** 厂商建议项：label 由调用方的翻译函数补上自建与自定义 */
export function vendorOptions(t: (key: string) => string): { label: string, value: string }[] {
  return VENDOR_VALUES.map(value => ({
    value,
    label: VENDOR_BRAND_LABELS[value] ?? t(value === 'SelfHosted'
      ? 'ai.providers.vendor_self_hosted'
      : 'ai.providers.vendor_custom'),
  }))
}

/** 编辑弹窗的表单：新建与编辑共用；API 密钥编辑时留空即保留原密钥 */
export interface ProviderForm {
  code: string
  name: string
  vendor: string
  chatModel: string
  embeddingModel: string
  endpoint: string
  apiKey: string
  maxOutputTokens: null | number
  temperature: null | number
  timeoutSeconds: null | number
  isDefault: boolean
  order: number
  status: ProviderStatus
  remark: string
}

export function emptyProviderForm(): ProviderForm {
  return {
    code: '',
    name: '',
    vendor: 'OpenAI',
    chatModel: '',
    embeddingModel: '',
    endpoint: '',
    apiKey: '',
    maxOutputTokens: null,
    temperature: null,
    timeoutSeconds: null,
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
 * 搜索字段按后端白名单下发：状态多选，关键字匹配编码、名称与聊天模型
 */
export function providersResource(): SchemaResource<ProviderRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await aiProviders.list(toPageRequest(params, {
        multiples: ['status'],
      })),
      params,
    ),
    remove: async id => throwOnFailed(await aiProviders.delete({
      ids: [
        String(id),
      ],
    })),
  }
}

/** 一次探测的结果行：成功给耗时（嵌入另给维度），失败给提供商的原因 */
export interface ProbeLine {
  kind: 'chat' | 'embedding'
  success: boolean
  elapsedMilliseconds: number
  message: null | string
  dimensions: null | number
}

/** 测试连接的结果拆成逐行（会话一行、嵌入一行，没配嵌入模型时嵌入一行说明不能用于知识库） */
export function probeLines(result: ProviderTestResult): { lines: ProbeLine[], embeddingAbsent: boolean } {
  const lines: ProbeLine[] = [
    {
      kind: 'chat',
      success: result.chat.success,
      elapsedMilliseconds: result.chat.elapsedMilliseconds,
      message: result.chat.message ?? null,
      dimensions: null,
    },
  ]
  if (result.embedding) {
    lines.push({
      kind: 'embedding',
      success: result.embedding.success,
      elapsedMilliseconds: result.embedding.elapsedMilliseconds,
      message: result.embedding.message ?? null,
      dimensions: result.embedding.dimensions ?? null,
    })
  }
  return { lines, embeddingAbsent: !result.embedding }
}

/**
 * 提供商的编辑弹窗：新建与编辑（编码只在新建时填、编辑时 API 密钥留空即保留，状态只在新建时选，之后走行内启停）
 * 详情不回显 API 密钥，只说有没有；字段级错误挂回对应字段
 */
export function useProviderEditor() {
  const open = ref(false)
  const editing = ref<null | ProviderDetail>(null)
  const form = reactive<ProviderForm>(emptyProviderForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)
  const isEdit = computed(() => editing.value !== null)

  function reset(next: Partial<ProviderForm> = {}) {
    Object.assign(form, emptyProviderForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate() {
    editing.value = null
    reset()
    open.value = true
  }

  async function startEdit(id: string) {
    const detail = await aiProviders.get({ id })
    editing.value = detail
    reset({
      code: detail.code,
      name: detail.name,
      vendor: detail.vendor,
      chatModel: detail.chatModel,
      embeddingModel: detail.embeddingModel ?? '',
      endpoint: detail.endpoint ?? '',
      maxOutputTokens: detail.maxOutputTokens ?? null,
      temperature: detail.temperature ?? null,
      timeoutSeconds: detail.timeoutSeconds ?? null,
      isDefault: detail.isDefault,
      order: detail.order,
      status: detail.status,
      remark: detail.remark ?? '',
    })
    open.value = true
  }

  function fieldError(field: keyof ProviderForm): string | undefined {
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
        vendor: form.vendor.trim(),
        chatModel: form.chatModel.trim(),
        embeddingModel: blankToNull(form.embeddingModel),
        endpoint: blankToNull(form.endpoint),
        apiKey: blankToNull(form.apiKey),
        maxOutputTokens: form.maxOutputTokens,
        temperature: form.temperature,
        timeoutSeconds: form.timeoutSeconds,
        isDefault: form.isDefault,
        order: form.order,
        remark: blankToNull(form.remark),
      }
      if (current) {
        await aiProviders.update({ id: current.id, concurrencyStamp: current.concurrencyStamp, ...common })
      }
      else {
        await aiProviders.create({
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
    startCreate,
    startEdit,
    submit,
  }
}
