import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { api } from '../api/client'
import type { Account } from '../api/types'
import { Avatar } from '../components/Avatar'
import { Badge } from '../components/Badge'
import { DataTable, type Column } from '../components/DataTable'
import { Icon } from '../components/Icon'
import { ErrorNotice } from '../components/Notice'
import { PageHeader } from '../components/PageHeader'
import { useAsync } from '../hooks/useAsync'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useListParams } from '../hooks/useListParams'

const columns: Column<Account>[] = [
  {
    field: 'HolderName',
    label: 'Titular',
    primary: true,
    render: (row) => (
      <span className="person">
        <Avatar name={row.holderName} />
        <strong>{row.holderName}</strong>
      </span>
    )
  },
  { field: 'HolderDocument', label: 'CPF', render: (row) => <span className="tabular">{row.holderDocument}</span> },
  { field: 'Status', label: 'Status', render: (row) => <Badge tone={row.statusTone} label={row.statusLabel} /> },
  {
    field: 'Balance',
    label: 'Saldo',
    numeric: true,
    render: (row) => <strong className={row.balance < 0 ? 'negative' : undefined}>{row.balanceDisplay}</strong>
  },
  { field: 'OverdraftLimit', label: 'Cheque especial', numeric: true, render: (row) => row.overdraftLimitDisplay },
  { field: 'AvailableToTransfer', label: 'Disponível', numeric: true, render: (row) => row.availableToTransferDisplay },
  { field: 'OpenedAt', label: 'Aberta em', render: (row) => <span className="muted">{row.openedAtDisplay}</span> }
]

export function AccountsPage() {
  const navigate = useNavigate()
  const list = useListParams('HolderName', 'Asc')
  const search = list.filter('search')
  const [term, setTerm] = useState(search)
  const debouncedTerm = useDebouncedValue(term)
  const { data, error, loading } = useAsync(
    () => api.listAccounts({ search, sortBy: list.sortBy, sortDirection: list.sortDirection, page: list.page }),
    [search, list.sortBy, list.sortDirection, list.page]
  )

  useEffect(() => {
    if (debouncedTerm !== search) list.setFilter('search', debouncedTerm)
  }, [debouncedTerm])

  return (
    <>
      <PageHeader
        title="Contas"
        subtitle="Saldo, cheque especial e valor disponível para transferir."
        actions={
          <Link to="/contas/nova" className="button button--primary">
            <Icon name="plus" size={16} />
            Nova conta
          </Link>
        }
      />
      <ErrorNotice error={error} />
      <DataTable
        label="Contas"
        columns={columns}
        page={data}
        list={list}
        loading={loading}
        rowKey={(row) => row.id}
        onRowClick={(row) => navigate(`/contas/${row.id}`)}
        emptyMessage={search ? 'Nenhuma conta encontrada para essa busca.' : 'Nenhuma conta cadastrada.'}
        toolbar={
          <label className="search">
            <Icon name="search" size={16} />
            <input
              type="search"
              aria-label="Buscar contas"
              placeholder="Buscar por nome ou CPF"
              value={term}
              onChange={(event) => setTerm(event.target.value)}
            />
          </label>
        }
      />
    </>
  )
}
