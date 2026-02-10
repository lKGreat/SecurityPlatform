import type { PagedRequest, PagedResult } from "@/types/api";
import { message } from "ant-design-vue";

export type FormMode = "create" | "edit";

export interface SelectOption {
  label: string;
  value: number;
}

export function debounce<T extends (...args: any[]) => void>(handler: T, delay = 300): T {
  let timer: number | undefined;
  return ((...args: any[]) => {
    if (timer) window.clearTimeout(timer);
    timer = window.setTimeout(() => handler(...args), delay);
  }) as unknown as T;
}

export interface LoadSelectOptionsConfig<TItem> {
  fetcher: (params: PagedRequest) => Promise<PagedResult<TItem>>;
  mapItem: (item: TItem) => SelectOption;
  pageSize?: number;
  errorMessage?: string;
}

export async function loadSelectOptions<TItem>(
  config: LoadSelectOptionsConfig<TItem>,
  keyword?: string
): Promise<SelectOption[]> {
  const { fetcher, mapItem, pageSize = 20, errorMessage = "加载选项失败" } = config;
  try {
    const result = await fetcher({
      pageIndex: 1,
      pageSize,
      keyword: keyword?.trim() || undefined
    });
    return result.items.map(mapItem);
  } catch (error) {
    message.error((error as Error).message || errorMessage);
    return [];
  }
}
