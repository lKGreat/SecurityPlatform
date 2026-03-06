import { requestApi } from '@/services/api-core'
import type {
  PluginDescriptor,
  PluginMarketEntry,
  PluginMarketSearchResult,
  PluginMarketVersion,
  PublishPluginRequest,
} from '@/types/plugin'
import type { ApiResponse } from '@/types/api'

// ─── 已安装插件管理 ───────────────────────────────────────────────────────────

export function getInstalledPlugins() {
  return requestApi<PluginDescriptor[]>('GET', '/api/v1/plugins')
}

export function reloadPlugins() {
  return requestApi<{ count: number }>('POST', '/api/v1/plugins/reload')
}

export function enablePlugin(code: string) {
  return requestApi<null>('POST', `/api/v1/plugins/${code}/enable`)
}

export function disablePlugin(code: string) {
  return requestApi<null>('POST', `/api/v1/plugins/${code}/disable`)
}

export function unloadPlugin(code: string) {
  return requestApi<null>('POST', `/api/v1/plugins/${code}/unload`)
}

export function installPluginPackage(file: File) {
  const form = new FormData()
  form.append('package', file)
  return requestApi<{ code: string; name: string; version: string }>(
    'POST',
    '/api/v1/plugins/install',
    form,
    { headers: {} } // let browser set content-type with boundary
  )
}

export function getPluginConfig(code: string, tenantId?: string, appId?: string) {
  const params = new URLSearchParams()
  if (tenantId) params.set('tenantId', tenantId)
  if (appId) params.set('appId', appId)
  return requestApi<{ configJson: string }>('GET', `/api/v1/plugins/${code}/config?${params}`)
}

export function savePluginConfig(
  code: string,
  scope: 'Global' | 'Tenant' | 'App',
  configJson: string,
  scopeId?: string
) {
  return requestApi<null>('PUT', `/api/v1/plugins/${code}/config`, { scope, scopeId, configJson })
}

// ─── 插件市场 ──────────────────────────────────────────────────────────────────

export function searchPluginMarket(params: {
  keyword?: string
  category?: string
  pageIndex?: number
  pageSize?: number
}) {
  const q = new URLSearchParams()
  if (params.keyword) q.set('keyword', params.keyword)
  if (params.category) q.set('category', params.category)
  q.set('pageIndex', String(params.pageIndex ?? 1))
  q.set('pageSize', String(params.pageSize ?? 20))
  return requestApi<PluginMarketSearchResult>('GET', `/api/v1/plugin-market?${q}`)
}

export function getPluginMarketEntry(code: string) {
  return requestApi<PluginMarketEntry>('GET', `/api/v1/plugin-market/${code}`)
}

export function getPluginMarketVersions(code: string) {
  return requestApi<PluginMarketVersion[]>('GET', `/api/v1/plugin-market/${code}/versions`)
}

export function publishPlugin(data: PublishPluginRequest) {
  return requestApi<{ id: number }>('POST', '/api/v1/plugin-market', data)
}

export function updatePluginMarketEntry(
  id: number,
  data: { name: string; description: string; iconUrl?: string }
) {
  return requestApi<null>('PUT', `/api/v1/plugin-market/${id}`, data)
}

export function deprecatePlugin(id: number) {
  return requestApi<null>('POST', `/api/v1/plugin-market/${id}/deprecate`)
}
