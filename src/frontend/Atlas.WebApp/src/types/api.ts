export interface ApiResponse<T> {
  success: boolean;
  code: string;
  message: string;
  traceId: string;
  data?: T;
}

export interface PagedRequest {
  pageIndex: number;
  pageSize: number;
  keyword?: string;
  sortBy?: string;
  sortDesc?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  pageIndex: number;
  pageSize: number;
}