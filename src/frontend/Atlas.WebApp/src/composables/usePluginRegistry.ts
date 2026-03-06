import { computed } from 'vue'
import {
  listFieldRenderers,
  getFieldRenderer,
  listValidators,
  getValidator,
  getMenuEntries,
  type PluginMenuEntry,
} from '@/plugins/registry'
import type { Component } from 'vue'

/**
 * 组合式函数：访问插件注册表
 */
export function usePluginRegistry() {
  const fieldRendererTypes = computed(() => listFieldRenderers())
  const validatorNames = computed(() => listValidators())
  const menuEntries = computed<PluginMenuEntry[]>(() => getMenuEntries())

  function resolveFieldRenderer(type: string): Component | undefined {
    return getFieldRenderer(type)
  }

  function resolveValidator(name: string) {
    return getValidator(name)
  }

  return {
    fieldRendererTypes,
    validatorNames,
    menuEntries,
    resolveFieldRenderer,
    resolveValidator,
  }
}
