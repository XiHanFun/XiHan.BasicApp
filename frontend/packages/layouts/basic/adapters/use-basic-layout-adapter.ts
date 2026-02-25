/**
 * @deprecated The basic layout adapter is no longer needed.
 * The new architecture handles theme/dark mode directly in BasicLayout.
 * Shell adapter (useLayoutShellAdapter) now provides all layout calculations.
 * This file is kept for backward compatibility only.
 */
export { useLayoutShellAdapter as useBasicLayoutAdapter } from '../composables'
