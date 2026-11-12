import { useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { ApiError, api } from '../api/client'
import type { Transfer } from '../api/types'
import { AccountPicker } from '../components/AccountPicker'
import { ErrorNotice, Notice } from '../components/Notice'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'

/** crypto.randomUUID só existe em contexto seguro (https ou localhost); fora dele, gera um UUID v4 com getRandomValues. */
function newIdempotencyKey(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID()
  const bytes = crypto.getRandomValues(new Uint8Array(16))
  bytes[6] = (bytes[6] & 0x0f) | 0x40
  bytes[8] = (bytes[8] & 0x3f) | 0x80
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('')
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`
}

export function NewTransferPage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const [scheduled, setScheduled] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<Error | null>(null)
  const [rejected, setRejected] = useState<Transfer | null>(null)
  const idempotencyKey = useRef(newIdempotencyKey())

  const renewKey = () => {
    idempotencyKey.current = newIdempotencyKey()
  }

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const scheduledFor = String(form.get('scheduledFor') ?? '')
    setSubmitting(true)
    setError(null)
    setRejected(null)
    try {
      const transfer = await api.createTransfer(
        {
          sourceAccountId: String(form.get('sourceAccountId')),
          destinationAccountId: String(form.get('destinationAccountId')),
          amount: Number(form.get('amount')),
          scheduledFor: scheduled && scheduledFor ? new Date(scheduledFor).toISOString() : undefined
        },
        idempotencyKey.current
      )
      navigate(`/transferencias/${transfer.id}`)
    } catch (reason) {
      if (reason instanceof ApiError && reason.problem.transfer) {
        setRejected(reason.problem.transfer)
      } else {
        setError(reason as Error)
      }
      renewKey()
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      <PageHeader
        back={{ to: '/transferencias', label: 'Transferências' }}
        title="Nova transferência"
        subtitle="Imediata ou agendada para uma data futura."
      />
      <div className="form-layout">
      <form className="card form" onSubmit={submit} onChange={renewKey}>
        <ErrorNotice error={error} />
        {rejected && (
          <Notice tone="danger" title="Transferência recusada">
            <p>{rejected.failureMessage}</p>
            <Link to={`/transferencias/${rejected.id}`}>Ver tentativa registrada</Link>
          </Notice>
        )}
        <div className="field-row">
          <AccountPicker
            name="sourceAccountId"
            label="Conta de origem"
            initialAccountId={params.get('source') ?? ''}
            detail={(account) => `CPF ${account.holderDocument} · disponível ${account.availableToTransferDisplay}`}
            onChange={renewKey}
          />
          <AccountPicker
            name="destinationAccountId"
            label="Conta de destino"
            detail={(account) => `CPF ${account.holderDocument}`}
            onChange={renewKey}
          />
        </div>
        <label className="field">
          <span>Valor (R$)</span>
          <span className="money-input">
            <span aria-hidden="true">R$</span>
            <input name="amount" type="number" min="0.01" step="0.01" required inputMode="decimal" placeholder="0,00" />
          </span>
        </label>
        <label className="switch">
          <span className="switch__control">
            <input type="checkbox" checked={scheduled} onChange={(event) => setScheduled(event.target.checked)} />
            <span className="switch__track" aria-hidden="true" />
          </span>
          <span className="switch__text">
            <span>Agendar para uma data futura</span>
            <small className="muted">A transferência é executada automaticamente na data escolhida.</small>
          </span>
        </label>
        {scheduled && (
          <label className="field">
            <span>Data e hora da execução</span>
            <input name="scheduledFor" type="datetime-local" required />
          </label>
        )}
        <div className="form__actions">
          <button type="button" className="button button--ghost" onClick={() => navigate(-1)}>
            Voltar
          </button>
          <button type="submit" className="button button--primary" disabled={submitting}>
            {submitting ? 'Enviando…' : scheduled ? 'Agendar transferência' : 'Transferir agora'}
          </button>
        </div>
      </form>
      <aside className="card card--muted tips">
        <h2>
          <Icon name="info" size={18} />
          Como funciona
        </h2>
        <ul>
          <li>
            <Icon name="wallet" size={16} />
            <span>O valor disponível inclui o saldo e o cheque especial da conta de origem.</span>
          </li>
          <li>
            <Icon name="clock" size={16} />
            <span>Há limites de valor e de tentativas por hora, diferentes nos períodos diurno e noturno.</span>
          </li>
          <li>
            <Icon name="calendar" size={16} />
            <span>Transferências agendadas podem ser canceladas enquanto não forem executadas.</span>
          </li>
        </ul>
      </aside>
      </div>
    </>
  )
}
