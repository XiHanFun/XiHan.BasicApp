/**
 * 数据源字段 → 打印设计器的字段树。
 * 职责：把代码数据源的字段目录换成设计器 fields 认的 DataField 树，属性面板里绑定字段时从这棵树里选。
 * 字段路径里的点号拆成对象层级；明细表是 list，列是它的子字段。
 */
import type { DataField } from '@xihan-ui/designer-kit'
import type { PrintFieldDefinition, PrintSampleInputType, PrintTableColumn } from './types'

/** 样例控件类型 → 字段的数据类型；其余按文本。 */
const VALUE_TYPES: Partial<Record<PrintSampleInputType, NonNullable<DataField['type']>>> = {
  boolean: 'boolean',
  date: 'date',
  datetime: 'datetime',
  number: 'number',
}

interface FieldNode {
  name: string
  label?: string
  kind: DataField['kind']
  type?: DataField['type']
  children?: FieldNode[]
}

function valueNode(name: string, label: string, inputType: PrintSampleInputType | undefined): FieldNode {
  const type = inputType ? VALUE_TYPES[inputType] : undefined
  return { name, label, kind: 'value', ...(type ? { type } : {}) }
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
      : valueNode(field.key, field.label, field.inputType)
    insert(root, field.key.split('.'), leaf)
  }
  return root
}
