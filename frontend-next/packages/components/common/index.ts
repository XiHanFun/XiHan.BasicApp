import { defineAsyncComponent } from 'vue'

// 通知正文渲染（Markdown 编辑器体量大）异步懒加载，不进首屏依赖图
export const NotificationContent = defineAsyncComponent(() => import('./NotificationContent.vue'))

export { indexDropdownOptions, toDropdownCollection } from './dropdown-collection'
export { default as XUserAvatar } from './UserAvatar.vue'
// 把 (row) => VNodeChild 这类渲染函数塞进模板的稳定壳子
export { VNodeRender } from './VNodeRender'
export { default as XColorPicker } from './XColorPicker.vue'
export { default as XDropdown } from './XDropdown.vue'
export { default as XIconButton } from './XIconButton.vue'
export { default as XInput } from './XInput.vue'
export { default as XNumberInput } from './XNumberInput.vue'
export { default as XSegmented } from './XSegmented.vue'
export { default as XSelect } from './XSelect.vue'
export { default as XSlider } from './XSlider.vue'
export { default as XTooltip } from './XTooltip.vue'
