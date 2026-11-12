import { useSearchParams } from 'react-router'
import type { SortDirection } from '../api/types'

export interface ListState {
  sortBy: string
  sortDirection: SortDirection
  page: number
  setSort: (field: string) => void
  setPage: (page: number) => void
  setFilter: (key: string, value: string) => void
  filter: (key: string) => string
}

export function useListParams(defaultSort: string, defaultDirection: SortDirection): ListState {
  const [params, setParams] = useSearchParams()
  const sortBy = params.get('sortBy') ?? defaultSort
  const sortDirection = (params.get('sortDirection') as SortDirection | null) ?? defaultDirection
  const page = Number(params.get('page') ?? '1')

  const update = (changes: Record<string, string>) => {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([key, value]) => (value ? next.set(key, value) : next.delete(key)))
    setParams(next, { replace: true })
  }

  return {
    sortBy,
    sortDirection,
    page,
    setSort: (field) =>
      update({
        sortBy: field,
        sortDirection: field === sortBy && sortDirection === 'Asc' ? 'Desc' : 'Asc',
        page: '1'
      }),
    setPage: (target) => update({ page: String(target) }),
    setFilter: (key, value) => update({ [key]: value, page: '1' }),
    filter: (key) => params.get(key) ?? ''
  }
}
