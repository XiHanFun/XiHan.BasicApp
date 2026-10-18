/**
 * 数据源字段 → 打印设计器的字段树，以及模板引用的字段与数据源的比对。
 * 职责：把代码数据源的字段目录换成设计器 fields 认的 DataField 树，物料面板的「字段」一组与属性面板绑定字段都从这棵树里来；
 * 列出模板引用了、数据源却没登记的字段。
 * 字段路径里的点号拆成对象层级；明细表是 list，列是它的子字段；图片、条码、二维码字段带上对应的 content。
 */
import type { PrintDocument } from '@xihan-print/core'
import type { DataField, DataFieldContent } from '@xihan-ui/designer-kit'
import type { PrintFieldDefinition, PrintFieldKind, PrintSampleInputType, PrintTableColumn } from './types'
import { collectDocumentFields } from '@xihan-print/core'

/** 样例控件类型 → 字段的数据类型；其余按文本。 */
const VALUE_TYPES: Partial<Record<PrintSampleInputType, NonNullable<DataField['type']>>> = {
  boolean: 'boolean',
  date: 'date',
  datetime: 'datetime',
  number: 'number',
}

/** 字段内容种类 → 设计器生成的元素；文本不写。 */
const CONTENTS: Partial<Record<PrintFieldKind, DataFieldContent>> = {
  barcode: 'barcode',
  image: 'image',
  qrcode: 'qrcode',
}

interface FieldNode {
  name: string
  label?: string
  kind: DataField['kind']
  type?: DataField['type']
  content?: DataFieldContent
  children?: FieldNode[]
}

function valueNode(name: string, label: string, inputType: PrintSampleInputType | undefined, kind?: PrintFieldKind): FieldNode {
  const type = inputType ? VALUE_TYPES[inputType] : undefined
  const content = kind ? CONTENTS[kind] : undefined
  return { name, label, kind: 'value', ...(type ? { type } : {}), ...(content ? { content } : {}) }
}

/** 按点号路径把叶子放进树里，中间层是 object。 */
function insert(siblings: FieldNode[], path: readonly string[], leaf: FieldNode): void {
  const [head, ...rest] = path
  if (head === undefined)
    return
  if (rest.length === 0) {
    siblings.push({ ...leaf, name: head })
    return
  }
  let parent = siblings.find(node => node.name === head && node.kind === 'object')
  if (!parent) {
    parent = { name: head, kind: 'object', children: [] }
    siblings.push(parent)
  }
  insert(parent.children!, rest, leaf)
}

function columnNodes(columns: readonly PrintTableColumn[]): FieldNode[] {
  const nodes: FieldNode[] = []
  for (const column of columns)
    insert(nodes, column.field.split('.'), valueNode(column.field, column.title, column.inputType))
  return nodes
}

/**
 * 把数据源字段目录换成设计器的字段树。
 * @param fields 已注册数据源的字段目录。
 * @returns 设计器 fields 属性用的字段树。
 */
export function toPrintDataFields(fields: readonly PrintFieldDefinition[]): DataField[] {
  const root: FieldNode[] = []
  for (const field of fields) {
    const leaf: FieldNode = field.kind === 'table'
      ? { name: field.key, label: field.label, kind: 'list', children: columnNodes(field.columns ?? []) }
      : valueNode(field.key, field.label, field.inputType, field.kind)
    insert(root, field.key.split('.'), leaf)
  }
  return root
}

/**
 * 列出模板引用了、数据源却没登记的字段。
 * @param document 打印模板。
 * @param fields 模板绑定的数据源的字段目录。
 * @returns 字段路径（点号写法），按在模板里首次出现的顺序；明细表的列写成「表.列」。
 */
export function listUnregisteredPrintFields(document: PrintDocument, fields: readonly PrintFieldDefinition[]): string[] {
  const missing: string[] = []
  const walk = (used: readonly DataField[], offered: readonly DataField[] | undefined, prefix: string): void => {
    for (const field of used) {
      const path = prefix ? `${prefix}.${field.name}` : field.name
      const match = offered?.find(item => item.name === field.name)
      if (!match)
        missing.push(path)
      else if (field.children)
        walk(field.children, match.children, path)
    }
  }
  walk(collectDocumentFields(document), toPrintDataFields(fields), '')
  return missing
}
