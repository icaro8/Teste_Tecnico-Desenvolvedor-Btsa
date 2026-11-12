import type { ReactNode } from 'react'
import { useNavigate } from 'react-router'
import type { PagedResponse, Transfer } from '../api/types'
import type { ListState } from '../hooks/useListParams'
import { Avatar } from './Avatar'
import { Badge } from './Badge'
import { DataTable, type Column } from './DataTable'

const party = (name: string) => (
  <span className="person">
    <Avatar name={name} size="sm" />
    <span>{name}</span>
  </span>
)

const columns: Column<Transfer>[] = [
  { field: 'CreatedAt', label: 'Criada em', render: (row) => <span className="muted">{row.timeline.createdAtDisplay}</span> },
  { field: 'Kind', label: 'Tipo', render: (row) => row.kindLabel },
  { field: 'Source', label: 'Origem', primary: true, render: (row) => party(row.source.holderName) },
  { field: 'Destination', label: 'Destino', render: (row) => party(row.destination.holderName) },
  { field: 'Amount', label: 'Valor', numeric: true, render: (row) => <strong>{row.amountDisplay}</strong> },
  { field: 'ScheduledFor', label: 'Agendada para', render: (row) => row.timeline.scheduledForDisplay ?? <span className="muted">—</span> },
  { field: 'Status', label: 'Status', render: (row) => <Badge tone={row.statusTone} label={row.statusLabel} /> }
]

interface TransfersTableProps {
  page: PagedResponse<Transfer> | null
  list: ListState
  loading: boolean
  toolbar?: ReactNode
}

export function TransfersTable({ page, list, loading, toolbar }: TransfersTableProps) {
  const navigate = useNavigate()
  return (
    <DataTable
      label="Transferências"
      columns={columns}
      page={page}
      list={list}
      loading={loading}
      toolbar={toolbar}
      rowKey={(row) => row.id}
      onRowClick={(row) => navigate(`/transferencias/${row.id}`)}
      emptyMessage="Nenhuma transferência encontrada."
    />
  )
}
