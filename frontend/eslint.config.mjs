import antfu from '@antfu/eslint-config'

export default antfu({
  vue: true,
  typescript: true,
  formatters: {
    css: true,
    html: true,
  },
  rules: {
    'no-console': 'warn',
    'vue/component-name-in-template-casing': ['error', 'PascalCase'],
    'vue/multi-word-component-names': 'off',
    '@typescript-eslint/no-explicit-any': 'warn',
  },
  ignores: [
    'src/types/auto-imports.d.ts',
    'src/types/components.d.ts',
    'dist/**',
    'node_modules/**',
  ],
})
