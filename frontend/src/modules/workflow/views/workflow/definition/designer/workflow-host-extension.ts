/**
 * 流程定义设计器挂进菜单栏的宿主操作：引擎定义 JSON 与返回列表写成一个扩展的命令，都落在「文件」菜单，返回列表也上工具栏。
 * 引擎定义 JSON 编辑的是保存进库的那份，设计器「编辑」菜单里自带的「编辑 JSON」编辑的是设计器格式；
 * 两者都只在开发者模式开着时有（设置的「基础」页里开）。
 * 保存用设计器内置的 save 命令（Ctrl / ⌘ + S），不在这里。
 *
 * 扩展对象建一次、引用不变；保存状态变了由宿主调设计器的 requestUpdate() 刷新可用。
 */
import type { FlowDesignerExtension } from '~/designers/flow'
import { ArrowLeftIcon, FileTextIcon } from '~/designers'
import { developerModeOf } from '~/designers/settings'

/** 宿主扩展的 id；命令 id 都以它加点开头 */
export const WORKFLOW_HOST_EXTENSION_ID = '@xihan/workflow-host'

export const WORKFLOW_HOST_COMMANDS = {
  engineJson: `${WORKFLOW_HOST_EXTENSION_ID}.engine-json`,
  backToList: `${WORKFLOW_HOST_EXTENSION_ID}.back-to-list`,
} as const

export interface WorkflowHostOptions {
  t: (key: string) => string
  /** 正在保存，不许离开 */
  saving: () => boolean
  openEngineJson: () => void
  backToList: () => void
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
      {
        id: WORKFLOW_HOST_COMMANDS.backToList,
        label: () => t('workflow.designer.back_to_list'),
        icon: ArrowLeftIcon,
        placement: ['menubar:file', 'toolbar'],
        enabled: () => !options.saving(),
        run: () => options.backToList(),
      },
    ],
  }
}
