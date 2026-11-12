import { Link } from 'react-router'
import { api } from '../api/client'
import { Icon } from '../components/Icon'
import { ErrorNotice } from '../components/Notice'
import { PageHeader } from '../components/PageHeader'
import { TransfersTable } from '../components/TransfersTable'
import { useAsync } from '../hooks/useAsync'
import { useListParams } from '../hooks/useListParams'

const statusFilters = [
  { value: '', label: 'Todos os status' },
  { value: 'Scheduled', label: 'Agendadas' },
  { value: 'Completed', label: 'Concluídas' },
  { value: 'Failed', label: 'Falharam' },
  { value: 'Cancelled', label: 'Canceladas' }
]

export function TransfersPage() {
  const list = useListParams('CreatedAt', 'Desc')
  const status = list.filter('status')
  const { data, error, loading } = useAsync(
    () => api.listTransfers({ status, sortBy: list.sortBy, sortDirection: list.sortDirection, page: list.page }),
    [status, list.sortBy, list.sortDirection, list.page]
  )

  return (
    <>
      <PageHeader
        title="Transferências"
        subtitle="Imediatas e agendadas, com o status de cada uma."
        actions={
          <Link to="/transferencias/nova" className="button button--primary">
            <Icon name="send" size={16} />
            Nova transferência
          </Link>
        }
      />
      <ErrorNotice error={error} />
      <TransfersTable
        page={data}
        list={list}
        loading={loading}
        toolbar={
          <label className="select-field">
            <span className="visually-hidden">Filtrar por status</span>
            <select value={status} onChange={(event) => list.setFilter('status', event.target.value)} aria-label="Filtrar por status">
              {statusFilters.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
        }
      />
    </>
  )
}
