import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { api } from '../api/client'
import { Avatar } from '../components/Avatar'
import { Badge, Loading } from '../components/Badge'
import { Icon } from '../components/Icon'
import { ErrorNotice, Notice } from '../components/Notice'
import { PageHeader } from '../components/PageHeader'
import { useAsync } from '../hooks/useAsync'

export function TransferDetailsPage() {
  const { id = '' } = useParams()
  const { data: transfer, error, reload } = useAsync(() => api.getTransfer(id), [id])
  const [cancelError, setCancelError] = useState<Error | null>(null)
  const [cancelling, setCancelling] = useState(false)

  const cancel = async () => {
    setCancelling(true)
    setCancelError(null)
    try {
      await api.cancelTransfer(id)
      reload()
    } catch (reason) {
      setCancelError(reason as Error)
    } finally {
      setCancelling(false)
    }
  }

  if (error) return <ErrorNotice error={error} />
  if (!transfer) return <Loading />

  const timeline = [
    { label: 'Criada', value: transfer.timeline.createdAtDisplay },
    { label: 'Agendada para', value: transfer.timeline.scheduledForDisplay },
    { label: 'Processada', value: transfer.timeline.processedAtDisplay },
    { label: 'Concluída', value: transfer.timeline.completedAtDisplay },
    { label: 'Falhou', value: transfer.timeline.failedAtDisplay },
    { label: 'Cancelada', value: transfer.timeline.cancelledAtDisplay }
  ].filter((step) => step.value)

  return (
    <>
      <PageHeader
        back={{ to: '/transferencias', label: 'Transferências' }}
        title={`Transferência ${transfer.kindLabel.toLowerCase()}`}
        subtitle={<span className="mono">{transfer.id}</span>}
        actions={
          transfer.canCancel && (
            <button type="button" className="button button--danger" onClick={cancel} disabled={cancelling}>
              {cancelling ? 'Cancelando…' : 'Cancelar agendamento'}
            </button>
          )
        }
      />
      <ErrorNotice error={cancelError} />
      {transfer.failureMessage && <Notice tone="danger" title="Transferência recusada">{transfer.failureMessage}</Notice>}

      <div className="transfer-layout">
        <section className="card transfer-hero">
          <div className="transfer-hero__amount">
            <span className="stat__label">Valor</span>
            <span className="transfer-hero__value" data-testid="amount">{transfer.amountDisplay}</span>
            <span data-testid="transfer-status">
              <Badge tone={transfer.statusTone} label={transfer.statusLabel} />
            </span>
          </div>
          <div className="parties">
            <div className="party">
              <Avatar name={transfer.source.holderName} size="lg" />
              <span className="stat__label">Origem</span>
              <Link to={`/contas/${transfer.source.accountId}`}>{transfer.source.holderName}</Link>
            </div>
            <span className="parties__arrow" aria-hidden="true">
              <Icon name="arrowRight" size={20} />
            </span>
            <div className="party">
              <Avatar name={transfer.destination.holderName} size="lg" />
              <span className="stat__label">Destino</span>
              <Link to={`/contas/${transfer.destination.accountId}`}>{transfer.destination.holderName}</Link>
            </div>
          </div>
        </section>

        <section className="card">
          <div className="card__header">
            <h2>Linha do tempo</h2>
          </div>
          <ol className="timeline">
            {timeline.map((step) => (
              <li key={step.label}>
                <span className="timeline__label">{step.label}</span>
                <time>{step.value}</time>
              </li>
            ))}
          </ol>
        </section>
      </div>
    </>
  )
}
