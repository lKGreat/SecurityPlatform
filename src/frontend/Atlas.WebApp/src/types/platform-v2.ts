export interface TenantAppInstanceListItem {
  id: string;
  appKey: string;
  name: string;
  status: string;
  version: number;
  description?: string;
  category?: string;
  icon?: string;
  publishedAt?: string;
}

export interface TenantAppInstanceDetail extends TenantAppInstanceListItem {
  dataSourceId?: string;
}

export interface TenantAppDataSourceBinding {
  tenantAppInstanceId: string;
  dataSourceId?: string;
  dataSourceName?: string;
  dbType?: string;
  dataSourceActive?: boolean;
  lastTestedAt?: string;
}

export interface ResourceCenterGroupEntry {
  resourceId: string;
  resourceName: string;
  resourceType: string;
  status?: string;
  description?: string;
}

export interface ResourceCenterGroupItem {
  groupKey: string;
  groupName: string;
  total: number;
  items: ResourceCenterGroupEntry[];
}
