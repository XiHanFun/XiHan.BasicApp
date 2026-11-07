import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { codegenTemplates, toPageRequest, toSchemaPageResult } from '~/contracts'

export type TemplateRow = ResponseOf<'codegen-templates.list'>['items'][number]
export type TemplateDetail = ResponseOf<'codegen-templates.get'>
export type WriteMode = TemplateRow['writeMode']
export type TemplateEngine = TemplateRow['templateEngine']
export type Artifact = ResponseOf<'codegen-tables.preview'>['files'][number]
export type ArtifactSide = Artifact['side']

export const WRITE_MODES: readonly WriteMode[] = ['AlwaysOverwrite', 'WriteOnce']
/** 引擎只有 Scriban */
export const TEMPLATE_ENGINES: readonly TemplateEngine[] = ['Scriban']

/**
 * SchemaPage 的资源：分页与删除
 * 生成模式、写入方式与状态多选合成 in；关键字匹配编码、名称与描述
 */
export function templatesResource(): SchemaResource<TemplateRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await codegenTemplates.list(toPageRequest(params, {
        multiples: ['templateType', 'writeMode', 'status'],
      })),
      params,
    ),
  }
}

/** 预览里的一个目录树节点：目录以路径前缀为键，文件以产物下标为键 */
export interface ArtifactTreeNode {
  value: string
  label: string
  /** 文件节点的产物下标；目录节点为空 */
  index?: number
  writeOnce?: boolean
  children?: ArtifactTreeNode[]
}

/**
 * 产物按路径组成目录树：路径逐段建目录，文件挂到末级目录下；目录在前、各自按名称排
 * entries 带上产物在完整列表里的下标，分栏过滤后选中态仍按它定位
 */
export function buildArtifactTree(entries: readonly { artifact: Artifact, index: number }[]): ArtifactTreeNode[] {
  const roots: ArtifactTreeNode[] = []
  const directories = new Map<string, ArtifactTreeNode>()
  for (const { artifact, index } of entries) {
    const segments = artifact.path.replace(/\\/g, '/').split('/').filter(Boolean)
    const fileName = segments.pop() ?? artifact.path
    let siblings = roots
    let prefix = ''
    for (const segment of segments) {
      prefix = prefix
        ? `${prefix}/${segment}`
        : segment
      let directory = directories.get(prefix)
      if (!directory) {
        directory = { value: `dir:${prefix}`, label: segment, children: [] }
        directories.set(prefix, directory)
        siblings.push(directory)
      }
      siblings = directory.children!
    }
    siblings.push({
      value: `file:${index}`,
      label: fileName,
      index,
      writeOnce: artifact.writeMode === 'WriteOnce',
    })
  }

  function sort(nodes: ArtifactTreeNode[]): ArtifactTreeNode[] {
    nodes.sort((a, b) => {
      const aIsDirectory = a.children !== undefined
      if (aIsDirectory !== (b.children !== undefined)) {
        return aIsDirectory
          ? -1
          : 1
      }
      return a.label.localeCompare(b.label)
    })
    for (const node of nodes) {
      if (node.children)
        sort(node.children)
    }
    return nodes
  }

  return sort(roots)
}

/** 文件类型图标（只用离线预载的 lucide / mdi 图标集） */
export function fileIconOf(fileName: string): string {
  switch (fileName.slice(fileName.lastIndexOf('.') + 1).toLowerCase()) {
    case 'cs':
      return 'mdi:language-csharp'
    case 'ts':
      return 'mdi:language-typescript'
    case 'vue':
      return 'mdi:vuejs'
    case 'json':
      return 'mdi:code-json'
    case 'md':
      return 'mdi:language-markdown'
    case 'csproj':
    case 'xml':
      return 'lucide:file-code'
    default:
      return 'lucide:file'
  }
}
