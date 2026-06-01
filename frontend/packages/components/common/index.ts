import { defineAsyncComponent } from 'vue'

// 重型编辑器（tiptap / md-editor-v3 / vanilla-jsoneditor）异步懒加载：
// 避免被公共 barrel 拉入主依赖图，配合 vite manualChunks 真正按需加载，减小首屏体积。
export const XJsonEditor = defineAsyncComponent(() => import('./JsonEditor.vue'))
export { default as XJsonViewer } from './JsonViewer.vue'
export const XMdEditor = defineAsyncComponent(() => import('./MdEditor.vue'))
export const XRichTextEditor = defineAsyncComponent(() => import('./RichTextEditor.vue'))

export { default as XSystemQueryPanel } from './SystemQueryPanel.vue'
