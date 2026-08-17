import { defineAsyncComponent } from 'vue'

// 重型编辑器（monaco / tiptap / md-editor-v3 / vanilla-jsoneditor）异步懒加载：
// 避免被公共 barrel 拉入主依赖图，配合 vite manualChunks 真正按需加载，减小首屏体积。
export const XCodeEditor = defineAsyncComponent(() => import('./CodeEditor.vue'))
export const XJsonEditor = defineAsyncComponent(() => import('./JsonEditor.vue'))
export const XMdEditor = defineAsyncComponent(() => import('./MdEditor.vue'))
export const NotificationContent = defineAsyncComponent(() => import('./NotificationContent.vue'))
export const XRichTextEditor = defineAsyncComponent(() => import('./RichTextEditor.vue'))

export { default as XContentEditorField } from './ContentEditorField.vue'
export { default as XContributionHeatmap } from './ContributionHeatmap.vue'
export { indexDropdownOptions, toDropdownCollection } from './dropdown-collection'
export { default as XEditModal } from './EditModal.vue'
export { default as XPageShell } from './PageShell.vue'
export type { PermissionGrantItem } from './permission-grant-panel'
export { default as XPermissionGrantPanel } from './PermissionGrantPanel.vue'
export { resolveSortMove } from './sortable'
export { default as XSortableItem } from './SortableItem.vue'
export { default as XUserAvatar } from './UserAvatar.vue'
// 把 (row) => VNodeChild 这类渲染函数塞进模板的稳定壳子
export { VNodeRender } from './VNodeRender'
export { default as XCascader } from './XCascader.vue'
export { default as XColorPicker } from './XColorPicker.vue'
export type { XDataTableColumn } from './XDataTable.vue'
export { default as XDataTable } from './XDataTable.vue'
export { default as XDatePicker } from './XDatePicker.vue'
export { default as XDropdown } from './XDropdown.vue'
export { default as XIconButton } from './XIconButton.vue'
export { default as XInput } from './XInput.vue'
export { default as XNumberInput } from './XNumberInput.vue'
export { default as XPopconfirm } from './XPopconfirm.vue'
export { default as XSegmented } from './XSegmented.vue'
export { default as XSelect } from './XSelect.vue'
export { default as XSlider } from './XSlider.vue'
export { default as XTagsInput } from './XTagsInput.vue'
export { default as XTooltip } from './XTooltip.vue'
export { default as XTree } from './XTree.vue'
export { default as XTreeSelect } from './XTreeSelect.vue'
// dnd-kit 的拖拽容器与事件类型统一经底层暴露：应用层不直接依赖 @dnd-kit/vue
export { DragDropProvider } from '@dnd-kit/vue'
export type { DragEndEvent } from '@dnd-kit/vue'
