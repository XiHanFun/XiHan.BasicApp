import type { PropType } from 'vue'
import { defineComponent, h } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'

export type StatusKind = 'forbidden' | 'not_found' | 'home'

/** 壳层自己的状态页（无权访问、页面不存在、没有可访问的页面）：不属于任何能力包，不走菜单。 */
export const StatusView = defineComponent({
  name: 'StatusView',
  props: {
    kind: { type: String as PropType<StatusKind>, required: true },
  },
  setup(props) {
    const { t } = useI18n()
    return () => h('section', { 'class': 'flex flex-col items-start gap-3 rounded-surface border border-border bg-surface p-6', 'data-testid': `status-${props.kind}` }, [
      h('h1', { class: 'text-heading-3 font-semibold' }, t(`common.status.${props.kind}`)),
      h('p', { class: 'text-caption text-fg-muted' }, t(`common.status.${props.kind}_hint`)),
      h(RouterLink, { to: '/', class: 'text-caption text-fg-brand underline-offset-2 hover:underline' }, () => t('common.actions.home')),
    ])
  },
})
