/**
 * 流程定义设计器挂进菜单栏的宿主操作：引擎定义 JSON 写成一个扩展的命令，落在「文件」菜单；回列表用设计器自带的关闭钮（closable）。
 * 引擎定义 JSON 编辑的是保存进库的那份，设计器「编辑」菜单里自带的「编辑 JSON」编辑的是设计器格式；
 * 两者都只在开发者模式开着时有（设置的「基础」页里开）。
 * 保存用设计器内置的 save 命令（Ctrl / ⌘ + S），不在这里。
 *
 * 扩展对象建一次、引用不变。
 */
import type { FlowDesignerExtension } from '~/designers/flow'
import { FileTextIcon } from '~/designers'
import { developerModeOf } from '~/designers/settings'

/** 宿主扩展的 id；命令 id 都以它加点开头 */
export const WORKFLOW_HOST_EXTENSION_ID = '@xihan/workflow-host'

export const WORKFLOW_HOST_COMMANDS = {
  engineJson: `${WORKFLOW_HOST_EXTENSION_ID}.engine-json`,
} as const

export interface WorkflowHostOptions {
  t: (key: string) => string
  openEngineJson: () => void
}

export function createWorkflowHostExtension(options: WorkflowHostOptions): FlowDesignerExtension {
  const { t } = options
  return {
    id: WORKFLOW_HOST_EXTENSION_ID,
    commands: [
      {
        id: WORKFLOW_HOST_COMMANDS.engineJson,
        label: () => t('workflow.designer.engine_json'),
        icon: FileTextIcon,
        placement: ['menubar:file'],
        visible: ({ settings }) => developerModeOf(settings),
        run: () => options.openEngineJson(),
      },
    ],
  }
}
