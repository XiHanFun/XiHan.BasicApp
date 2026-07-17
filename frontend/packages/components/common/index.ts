import { defineAsyncComponent } from 'vue'

// 重型编辑器（tiptap / md-editor-v3 / vanilla-jsoneditor）异步懒加载：
// 避免被公共 barrel 拉入主依赖图，配合 vite manualChunks 真正按需加载，减小首屏体积。
export const XJsonEditor = defineAsyncComponent(() => import('./JsonEditor.vue'))
export const XMdEditor = defineAsyncComponent(() => import('./MdEditor.vue'))
export const NotificationContent = defineAsyncComponent(() => import('./NotificationContent.vue'))
export const XRichTextEditor = defineAsyncComponent(() => import('./RichTextEditor.vue'))

export { default as XEditModal } from './EditModal.vue'
export { default as XPageShell } from './PageShell.vue'
export { resolveSortMove } from './sortable'
export { default as XSortableItem } from './SortableItem.vue'
export { default as XUserAvatar } from './UserAvatar.vue'
// dnd-kit 的拖拽容器与事件类型统一经底层暴露：应用层不直接依赖 @dnd-kit/vue
export { DragDropProvider } from '@dnd-kit/vue'
export type { DragEndEvent } from '@dnd-kit/vue'
