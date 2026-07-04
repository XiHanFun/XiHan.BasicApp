/**
 * 知识库枚举（与后端全局 JsonStringEnumConverter 序列化值一致，成员名即 wire value）。
 */

/** 知识文档来源类型 */
export enum KnowledgeSourceType {
  PasteText = 'PasteText',
  UploadFile = 'UploadFile',
}

/** 知识文档索引状态 */
export enum KnowledgeIndexStatus {
  Pending = 'Pending',
  Indexed = 'Indexed',
  Failed = 'Failed',
}

/** 来源类型选项 */
export const KNOWLEDGE_SOURCE_TYPE_OPTIONS = [
  { label: '粘贴文本', value: KnowledgeSourceType.PasteText },
  { label: '上传文件', value: KnowledgeSourceType.UploadFile },
]

/** 索引状态选项 */
export const KNOWLEDGE_INDEX_STATUS_OPTIONS = [
  { label: '待索引', value: KnowledgeIndexStatus.Pending },
  { label: '已索引', value: KnowledgeIndexStatus.Indexed },
  { label: '失败', value: KnowledgeIndexStatus.Failed },
]
