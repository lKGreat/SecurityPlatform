import type { Component } from 'vue'

/**
 * 全局插件组件注册表
 * 供插件动态注册自定义字段类型、渲染器、验证器等
 */

// ─── 字段渲染器 ────────────────────────────────────────────────────────────────

type FieldRenderer = Component
const _fieldRenderers = new Map<string, FieldRenderer>()

export function registerFieldRenderer(type: string, component: FieldRenderer): void {
  _fieldRenderers.set(type, component)
}

export function getFieldRenderer(type: string): FieldRenderer | undefined {
  return _fieldRenderers.get(type)
}

export function listFieldRenderers(): string[] {
  return [..._fieldRenderers.keys()]
}

// ─── 表格单元格渲染器 ──────────────────────────────────────────────────────────

type GridCellRenderer = Component
const _gridCellRenderers = new Map<string, GridCellRenderer>()

export function registerGridCellRenderer(type: string, component: GridCellRenderer): void {
  _gridCellRenderers.set(type, component)
}

export function getGridCellRenderer(type: string): GridCellRenderer | undefined {
  return _gridCellRenderers.get(type)
}

// ─── 验证器 ────────────────────────────────────────────────────────────────────

type ValidatorFn = (value: unknown, config?: Record<string, unknown>) => string | null

const _validators = new Map<string, ValidatorFn>()

export function registerValidator(name: string, fn: ValidatorFn): void {
  _validators.set(name, fn)
}

export function getValidator(name: string): ValidatorFn | undefined {
  return _validators.get(name)
}

export function listValidators(): string[] {
  return [..._validators.keys()]
}

// ─── 侧边栏菜单扩展 ──────────────────────────────────────────────────────────

export interface PluginMenuEntry {
  key: string
  label: string
  icon?: Component
  path: string
  parentKey?: string
  order?: number
}

const _menuEntries: PluginMenuEntry[] = []

export function registerMenuEntry(entry: PluginMenuEntry): void {
  if (!_menuEntries.find((m) => m.key === entry.key)) {
    _menuEntries.push(entry)
  }
}

export function getMenuEntries(): PluginMenuEntry[] {
  return [..._menuEntries].sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
}

// ─── 工具函数：清空注册表（仅测试用）──────────────────────────────────────────

export function _clearRegistryForTesting(): void {
  _fieldRenderers.clear()
  _gridCellRenderers.clear()
  _validators.clear()
  _menuEntries.length = 0
}
