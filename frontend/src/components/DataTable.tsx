import type { ReactNode } from 'react'
import type { PagedResponse } from '../api/types'
import type { ListState } from '../hooks/useListParams'
import { Icon } from './Icon'

export interface Column<T> {
  field: string
  label: string
  numeric?: boolean
  /** Coluna usada como título do card no layout mobile. */
  primary?: boolean
  render: (row: T) => ReactNode
}

interface DataTableProps<T> {
  label: string
  columns: Column<T>[]
  page: PagedResponse<T> | null
  list: ListState
  rowKey: (row: T) => string
  onRowClick?: (row: T) => void
  loading: boolean
  emptyMessage: string
  toolbar?: ReactNode
}

export function DataTable<T>({ label, columns, page, list, rowKey, onRowClick, loading, emptyMessage, toolbar }: DataTableProps<T>) {
  const rows = page?.items ?? []
  const showSkeleton = loading && rows.length === 0

  return (
    <div className="table-card">
      {toolbar && <div className="table-card__toolbar">{toolbar}</div>}
      <div className={`table-scroll${loading && rows.length > 0 ? ' is-refreshing' : ''}`}>
        <table aria-label={label} aria-busy={loading}>
          <thead>
            <tr>
              {columns.map((column) => {
                const active = list.sortBy === column.field
                const direction = active ? (list.sortDirection === 'Asc' ? 'ascending' : 'descending') : 'none'
                return (
                  <th key={column.field} aria-sort={direction} className={column.numeric ? 'numeric' : undefined} scope="col">
                    <button type="button" className={`sort-button${active ? ' sort-button--active' : ''}`} onClick={() => list.setSort(column.field)}>
                      <span>{column.label}</span>
                      <span className="sort-indicator" aria-hidden="true">
                        {direction === 'descending' ? '↓' : '↑'}
                      </span>
                    </button>
                  </th>
                )
              })}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr
                key={rowKey(row)}
                className={onRowClick ? 'clickable' : undefined}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                tabIndex={onRowClick ? 0 : undefined}
                onKeyDown={onRowClick ? (event) => event.key === 'Enter' && onRowClick(row) : undefined}
              >
                {columns.map((column) => (
                  <td
                    key={column.field}
                    className={[column.numeric && 'numeric', column.primary && 'primary'].filter(Boolean).join(' ') || undefined}
                    data-label={column.label}
                  >
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))}
            {!loading && rows.length === 0 && (
              <tr>
                <td colSpan={columns.length} className="empty">
                  <Icon name="search" size={28} />
                  <span>{emptyMessage}</span>
                </td>
              </tr>
            )}
          </tbody>
        </table>
        {showSkeleton && (
          <div className="table-skeleton" aria-hidden="true">
            {Array.from({ length: 5 }, (_, index) => (
              <span key={index} className="skeleton" />
            ))}
          </div>
        )}
      </div>
      {page && page.totalItems > 0 && (
        <nav className="pagination" aria-label="Paginação">
          <span className="pagination__info">
            {page.totalPages > 1 ? `Página ${page.page} de ${page.totalPages} · ` : ''}
            {page.totalItems} {page.totalItems === 1 ? 'registro' : 'registros'}
          </span>
          {page.totalPages > 1 && (
            <div className="pagination__buttons">
              <button type="button" className="button button--ghost button--sm" disabled={page.page <= 1} onClick={() => list.setPage(page.page - 1)}>
                <Icon name="chevronLeft" size={16} />
                Anterior
              </button>
              <button
                type="button"
                className="button button--ghost button--sm"
                disabled={page.page >= page.totalPages}
                onClick={() => list.setPage(page.page + 1)}
              >
                Próxima
                <Icon name="chevronRight" size={16} />
              </button>
            </div>
          )}
        </nav>
      )}
    </div>
  )
}
