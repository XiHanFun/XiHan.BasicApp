<script lang="ts">
import type { PropType } from 'vue'
import { computed, defineComponent } from 'vue'
import { usePermission } from '~/hooks/usePermission'

export default defineComponent({
  name: 'AccessControl',
  props: {
    /** 权限码（满足其一即可） */
    codes: { type: Array as PropType<string[]>, default: () => [] },
    /** 角色码（满足其一即可） */
    roles: { type: Array as PropType<string[]>, default: () => [] },
    /** one-of：codes 或 roles 其一满足；all-of：两者都需满足（空数组视为不限制） */
    mode: { type: String as PropType<'one-of' | 'all-of'>, default: 'one-of' },
  },
  setup(props, { slots }) {
    const { hasPermission, hasRole } = usePermission()

    const hasAccess = computed(() => {
      const hasCodes = props.codes.length === 0 || hasPermission(props.codes)
      const hasRoles = props.roles.length === 0 || hasRole(props.roles)

      if (props.codes.length === 0 && props.roles.length === 0)
        return true
      if (props.mode === 'all-of')
        return hasCodes && hasRoles
      return hasCodes || hasRoles
    })

    return () => (hasAccess.value ? slots.default?.() : null)
  },
})
</script>
