/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { reactive, ref } from 'vue'
import { knowledgeDocuments, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError, LONG_TIMEOUT_MS, throwOnFailed } from '~/request'

export type KnowledgeRow = ResponseOf<'knowledge-documents.list'>['items'][number]
export type KnowledgeDetail = ResponseOf<'knowledge-documents.get'>
export type KnowledgeCitation = ResponseOf<'knowledge-documents.search'>['citations'][number]
export type KnowledgeSourceType = KnowledgeRow['sourceType']
export type KnowledgeIndexStatus = KnowledgeRow['status']

/** 索引状态的标签色 */
export const STATUS_TONE = { Pending: 'warning', Indexed: 'success', Failed: 'danger' } as const

/** 索引状态的文案键 */
export function statusLabelKey(status: KnowledgeIndexStatus): string {
  if (status === 'Indexed')
    return 'ai.knowledge_documents.status_indexed'
  return status === 'Failed'
    ? 'ai.knowledge_documents.status_failed'
    : 'ai.knowledge_documents.status_pending'
}

/** 来源类型的文案键 */
export function sourceTypeLabelKey(type: KnowledgeSourceType): string {
  return type === 'UploadFile'
    ? 'ai.knowledge_documents.source_upload'
    : 'ai.knowledge_documents.source_paste'
}

/** 摄取弹窗能读的文件（前端读成文本提交，不限大小） */
export const INGEST_FILE_ACCEPT = '.txt,.md,.markdown,.cs,.ts,.vue,.js,.json,.py,.java,.go,.sql,.yml,.yaml,.html,.css'

/** 摄取弹窗的表单 */
export interface IngestForm {
  title: string
  sourceType: KnowledgeSourceType
  source: string
  text: string
  embeddingProviderCode: string
  remark: string
}

export function emptyIngestForm(): IngestForm {
  return {
    title: '',
    sourceType: 'PasteText',
    source: '',
    text: '',
    embeddingProviderCode: '',
    remark: '',
  }
}

function blankToNull(value: string): null | string {
  return value.trim() === ''
    ? null
    : value.trim()
}

/**
 * SchemaPage 的资源：分页与删除，关键字匹配标题与来源
 * 只看调用方一侧
 */
export function knowledgeDocumentsResource(): SchemaResource<KnowledgeRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await knowledgeDocuments.list(toPageRequest(params, {
        multiples: ['sourceType', 'status'],
      })),
      params,
    ),
    remove: async id => throwOnFailed(await knowledgeDocuments.delete({
      ids: [
        String(id),
      ],
    })),
  }
}

/**
 * 摄取弹窗：粘贴文本或从文件读取
 * 读到文件后来源记文件名，标题为空即取文件名
 * 索引失败时文档照样保存并成功返回，页面按状态提示
 */
export function useKnowledgeIngest() {
  const open = ref(false)
  const form = reactive<IngestForm>(emptyIngestForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  function startCreate() {
    Object.assign(form, emptyIngestForm())
    fieldErrors.value = {}
    submitError.value = null
    open.value = true
  }

  function applyFile(name: string, text: string) {
    form.text = text
    form.source = name
    form.sourceType = 'UploadFile'
    if (form.title.trim() === '')
      form.title = name
  }

  function fieldError(field: keyof IngestForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<KnowledgeDetail | null> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const document = await knowledgeDocuments.ingest({
        title: form.title.trim(),
        text: form.text,
        sourceType: form.sourceType,
        source: blankToNull(form.source),
        embeddingProviderCode: blankToNull(form.embeddingProviderCode),
        order: 0,
        remark: blankToNull(form.remark),
      }, { timeoutMs: LONG_TIMEOUT_MS })
      open.value = false
      return document
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
      return null
    }
    finally {
      submitting.value = false
    }
  }

  return {
    open,
    form,
    fieldError,
    submitError,
    submitting,
    startCreate,
    applyFile,
    submit,
  }
}

/**
 * 检索试玩，只在调用方一侧检索
 * 答案里的 [n] 对应列表里第 n 个片段
 */
export function useKnowledgePlayground() {
  const question = ref('')
  const top = ref(5)
  const provider = ref('')
  const wantAnswer = ref(true)
  const loading = ref(false)
  const answer = ref<null | string>(null)
  const citations = ref<KnowledgeCitation[]>([])
  const searched = ref(false)

  /**
   * 要答案走 `knowledge-documents.query`，只要片段走 `knowledge-documents.search`
   * 没填问题不发请求，返回 false；请求失败抛出，由页面提示
   */
  async function run(): Promise<boolean> {
    const text = question.value.trim()
    if (text === '')
      return false
    loading.value = true
    try {
      const request = {
        question: text,
        top: top.value,
        provider: blankToNull(provider.value),
      }
      if (wantAnswer.value) {
        const result = await knowledgeDocuments.query({ ...request, answer: true }, { timeoutMs: LONG_TIMEOUT_MS })
        answer.value = result.answer ?? null
        citations.value = result.citations
      }
      else {
        const result = await knowledgeDocuments.search(request, { timeoutMs: LONG_TIMEOUT_MS })
        answer.value = null
        citations.value = result.citations
      }
      searched.value = true
      return true
    }
    finally {
      loading.value = false
    }
  }

  return {
    question,
    top,
    provider,
    wantAnswer,
    loading,
    answer,
    citations,
    searched,
    run,
  }
}

/** 片段的显示名：标题，没有即来源，再没有即文档 ID */
export function citationTitle(citation: KnowledgeCitation): string {
  return citation.title || citation.source || citation.documentId
}

/**
 * 文档详情：按 ID 取原文与索引状态
 * 从命中片段打开时带上片段正文，原文里找得到就标出来；连着打开两次时只认后一次
 */
export function useKnowledgeDetail() {
  const open = ref(false)
  const loading = ref(false)
  const document = ref<KnowledgeDetail | null>(null)
  const hit = ref<null | string>(null)
  let latest = 0

  /** 加载失败关掉抽屉并抛出，由页面提示 */
  async function show(id: string, hitText: null | string = null) {
    const current = ++latest
    document.value = null
    hit.value = hitText
    open.value = true
    loading.value = true
    try {
      const loaded = await knowledgeDocuments.get({ id })
      if (current === latest)
        document.value = loaded
    }
    catch (error) {
      if (current === latest)
        open.value = false
      throw error
    }
    finally {
      if (current === latest)
        loading.value = false
    }
  }

  return {
    open,
    loading,
    document,
    hit,
    show,
  }
}

/** 原文按命中片段切成三段；没有片段或原文里找不到时整段都在 `before` */
export function splitByHit(text: string, hit: null | string): { before: string, hit: string, after: string } {
  const needle = hit?.trim() ?? ''
  const at = needle === ''
    ? -1
    : text.indexOf(needle)
  return at < 0
    ? { before: text, hit: '', after: '' }
    : {
        before: text.slice(0, at),
        hit: needle,
        after: text.slice(at + needle.length),
      }
}
