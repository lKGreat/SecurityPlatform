import { requestApi } from '@/services/api-core'

export interface WebhookSubscription {
  id: number
  name: string
  eventTypes: string[]
  targetUrl: string
  secret: string
  headers?: Record<string, string>
  isActive: boolean
  createdAt: string
  lastTriggeredAt?: string
  tenantId: string
}

export interface WebhookDeliveryLog {
  id: number
  subscriptionId: number
  eventType: string
  payload: string
  responseCode?: number
  responseBody?: string
  durationMs: number
  success: boolean
  errorMessage?: string
  createdAt: string
}

export function getWebhooks() {
  return requestApi<WebhookSubscription[]>('GET', '/api/v1/webhooks')
}

export function getWebhook(id: number) {
  return requestApi<WebhookSubscription>('GET', `/api/v1/webhooks/${id}`)
}

export function createWebhook(data: {
  name: string
  eventTypes: string[]
  targetUrl: string
  secret: string
  headers?: Record<string, string>
}) {
  return requestApi<{ id: number }>('POST', '/api/v1/webhooks', data)
}

export function updateWebhook(
  id: number,
  data: {
    name: string
    eventTypes: string[]
    targetUrl: string
    isActive: boolean
    headers?: Record<string, string>
  }
) {
  return requestApi<null>('PUT', `/api/v1/webhooks/${id}`, data)
}

export function deleteWebhook(id: number) {
  return requestApi<null>('DELETE', `/api/v1/webhooks/${id}`)
}

export function getWebhookDeliveries(id: number, pageSize = 50) {
  return requestApi<WebhookDeliveryLog[]>('GET', `/api/v1/webhooks/${id}/deliveries?pageSize=${pageSize}`)
}

export function testWebhookDelivery(id: number) {
  return requestApi<null>('POST', `/api/v1/webhooks/${id}/test`)
}
