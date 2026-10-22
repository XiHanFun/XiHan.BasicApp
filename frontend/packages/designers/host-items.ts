/**
 * 宿主放进设计器外框的两样：菜单栏正中的标题、状态栏右端的一样。关闭钮用设计器自带的（closable）。
 *
 * 都作为设计器元素的直接子元素放进去，写原生 Web Components 的 slot 属性由设计器转交；元素常驻，只换文字。
 * 流程、打印两款编辑器共用，长相一致。
 */

import type { PropType } from 'vue'
import { computed, defineComponent, h } from 'vue'

/**
 * 菜单栏正中的标题：正在编辑的是哪一份，名称与编码合成一行（「名称 · 编码」），悬停看全文。
 * 一个元素整体截断：外壳按整条菜单栏居中，放不下时截到两边留出的空当并加省略号，再窄就整段藏起。
 */
export const DesignerMenubarTitle = defineComponent({
  name: 'DesignerMenubarTitle',
  props: {
    /** 名称：模板名、流程名；还没起名时由宿主给一个缺省叫法 */
    name: { type: String, required: true },
    /** 编码 */
    code: { type: String, default: undefined },
  },
  setup(props) {
    const text = computed(() => props.code ? `${props.name} · ${props.code}` : props.name)
    return () => h('span', { slot: 'menubar-center', class: 'designer-menubar-title', title: text.value }, text.value)
  },
})

/**
 * 状态栏右端的一样（如所绑数据源、保存状态）：外壳把它排在各款自己的状态项（缩放比例等）左边，
 * 相邻两样之间画细分隔；状态栏在设置里关掉时跟着不显示。
 */
export const DesignerStatusText = defineComponent({
  name: 'DesignerStatusText',
  props: {
    text: { type: String, required: true },
    /** 悬停说明，如这一样是什么 */
    title: { type: String, default: undefined },
    /** 提醒类（如未保存）用警示语气 */
    tone: { type: String as PropType<'warning'>, default: undefined },
  },
  setup(props) {
    return () => h('span', {
      'slot': 'status-end',
      'class': 'designer-status-text',
      'title': props.title,
      'data-tone': props.tone,
      'style': props.tone === 'warning' ? { color: 'var(--xh-fg-warning)' } : undefined,
    }, props.text)
  },
})
