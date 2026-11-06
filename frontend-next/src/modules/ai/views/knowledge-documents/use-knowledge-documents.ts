import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { reactive, ref } from 'vue'
import { knowledgeDocuments, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError, throwOnFailed } from '~/request'

export type KnowledgeRow = ResponseOf<'knowledge-documents.list'>['items'][number]
export type KnowledgeDetail = ResponseOf<'knowledge-documents.get'>
export type KnowledgeCitation = ResponseOf<'knowledge-documents.search'>['citations'][number]
export type KnowledgeSourceType = KnowledgeRow['sourceType']
export type KnowledgeIndexStatus = KnowledgeRow['status']

/** 摄取弹窗能读的文件（照 4.x：前端读成文本提交，不限大小） */
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
 * SchemaPage 的资源：分页与删除
 * 搜索字段按后端白名单下发：来源类型与状态多选，关键字匹配标题与来源；只看调用方一侧
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
 * 摄取弹窗（照 4.x）：粘贴文本或从文件读取，读到文件后来源记文件名、标题为空即取文件名
 * 字段级错误挂回对应字段；索引失败文档照样保存并成功返回，页面按状态提示
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
      })
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
 * 检索试玩（照 4.x）：提问、召回数、提供商与是否生成答案；只在调用方一侧检索
 * 引用的编号是片段在列表里的序号：答案里的 [n] 对应第 n 个片段
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

  /** 没填问题不发请求，返回 false；请求失败抛出，由页面提示 */
  async function run(): Promise<boolean> {
    const text = question.value.trim()
    if (text === '')
      return false
    loading.value = true
    try {
      const result = await knowledgeDocuments.query({
        question: text,
        top: top.value,
        provider: blankToNull(provider.value),
        answer: wantAnswer.value,
      })
      answer.value = result.answer ?? null
      citations.value = result.citations
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

/** 片段的显示名：标题，没有即来源，再没有即文档 ID（照 4.x） */
export function citationTitle(citation: KnowledgeCitation): string {
  return citation.title || citation.source || citation.documentId
}
