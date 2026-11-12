import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router'
import { api, type AccountStatusAction } from '../api/client'
import { Avatar } from '../components/Avatar'
import { Badge, Loading } from '../components/Badge'
import { Icon } from '../components/Icon'
import { ErrorNotice, Notice } from '../components/Notice'
import { PageHeader } from '../components/PageHeader'
import { TransfersTable } from '../components/TransfersTable'
import { useAsync } from '../hooks/useAsync'
import { useListParams } from '../hooks/useListParams'

const statusActions: { action: AccountStatusAction; label: string; flag: 'canBlock' | 'canDeactivate' | 'canActivate' }[] = [
  { action: 'block', label: 'Bloquear', flag: 'canBlock' },
  { action: 'deactivate', label: 'Inativar', flag: 'canDeactivate' },
  { action: 'activate', label: 'Ativar', flag: 'canActivate' }
]

export function AccountDetailsPage() {
  const { id = '' } = useParams()
  const list = useListParams('CreatedAt', 'Desc')
  const [actionError, setActionError] = useState<Error | null>(null)
  const [saved, setSaved] = useState<string | null>(null)
  const details = useAsync(() => api.getAccount(id), [id])
  const transfers = useAsync(
    () => api.listTransfers({ accountId: id, sortBy: list.sortBy, sortDirection: list.sortDirection, page: list.page }),
    [id, list.sortBy, list.sortDirection, list.page]
  )

  const run = async (operation: () => Promise<unknown>, successMessage: string | null = null) => {
    setActionError(null)
    setSaved(null)
    try {
      await operation()
      setSaved(successMessage)
      details.reload()
    } catch (reason) {
      setActionError(reason as Error)
    }
  }

  const changeOverdraft = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const value = Number(new FormData(event.currentTarget).get('overdraftLimit'))
    void run(() => api.changeOverdraftLimit(id, value), 'Limite de cheque especial atualizado.')
  }

  const changeTransferLimits = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const read = (name: string) => Number(form.get(name))
    void run(
      () =>
        api.changeTransferLimits(
          id,
          { maxAmount: read('daytimeAmount'), maxAttempts: read('daytimeAttempts') },
          { maxAmount: read('nighttimeAmount'), maxAttempts: read('nighttimeAttempts') }
        ),
      'Limites por hora atualizados.'
    )
  }

  if (details.error) return <ErrorNotice error={details.error} />
  if (!details.data) return <Loading />

  const { account, limits } = details.data

  const attemptsTotal = limits.usage.attemptsInLastHour + limits.usage.remainingAttempts
  const attemptsUsed = attemptsTotal > 0 ? (limits.usage.attemptsInLastHour / attemptsTotal) * 100 : 0

  return (
    <>
      <PageHeader
        back={{ to: '/', label: 'Contas' }}
        leading={<Avatar name={account.holderName} size="lg" />}
        title={account.holderName}
        subtitle={
          <>
            <Badge tone={account.statusTone} label={account.statusLabel} />
            <span className="tabular">CPF {account.holderDocument}</span>
            <span>Aberta em {account.openedAtDisplay}</span>
          </>
        }
        actions={
          <>
            {statusActions
              .filter((item) => account.actions[item.flag])
              .map((item) => (
                <button key={item.action} type="button" className="button button--ghost" onClick={() => run(() => api.changeAccountStatus(id, item.action))}>
                  {item.label}
                </button>
              ))}
            {account.actions.canSendTransfers && (
              <Link to={`/transferencias/nova?source=${account.id}`} className="button button--primary">
                <Icon name="send" size={16} />
                Transferir
              </Link>
            )}
          </>
        }
      />
      <ErrorNotice error={actionError} />
      {saved && <Notice tone="success">{saved}</Notice>}

      <section className="stats" aria-label="Resumo da conta">
        <div className="stat stat--hero">
          <span className="stat__icon"><Icon name="wallet" /></span>
          <span className="stat__label">Disponível para transferir</span>
          <span className="stat__value" data-testid="available">{account.availableToTransferDisplay}</span>
          <span className="stat__hint">Saldo + cheque especial livre</span>
        </div>
        <div className="stat">
          <span className="stat__icon"><Icon name="accounts" /></span>
          <span className="stat__label">Saldo</span>
          <span className={`stat__value${account.balance < 0 ? ' negative' : ''}`} data-testid="balance">{account.balanceDisplay}</span>
        </div>
        <div className="stat">
          <span className="stat__icon"><Icon name="shield" /></span>
          <span className="stat__label">Cheque especial em uso</span>
          <span className="stat__value" data-testid="overdraft-in-use">{account.overdraftInUseDisplay}</span>
          <span className="stat__hint">de {account.overdraftLimitDisplay}</span>
        </div>
      </section>

      <section className="grid-2">
        <div className="card">
          <div className="card__header">
            <h2>Limites por hora</h2>
            <span className="pill">
              <Icon name="clock" size={14} />
              Período atual: <strong data-testid="current-period">{limits.currentPeriodLabel}</strong>
            </span>
          </div>
          <div className="meter">
            <div className="meter__labels">
              <span>Tentativas na última hora</span>
              <span className="tabular">
                <span data-testid="remaining-attempts">{limits.usage.remainingAttempts}</span> restantes de {attemptsTotal}
              </span>
            </div>
            <div className="meter__track" role="presentation">
              <span className="meter__fill" style={{ width: `${attemptsUsed}%` }} />
            </div>
          </div>
          <dl className="details">
            <div>
              <dt>Transferido na última hora</dt>
              <dd>{limits.usage.transferredInLastHourDisplay}</dd>
            </div>
            <div>
              <dt>Ainda disponível nesta hora</dt>
              <dd>{limits.usage.remainingAmountDisplay}</dd>
            </div>
            <div>
              <dt>Diurno</dt>
              <dd>
                {limits.daytime.maxAmountDisplay} · {limits.daytime.maxAttempts} tentativas
              </dd>
            </div>
            <div>
              <dt>Noturno</dt>
              <dd>
                {limits.nighttime.maxAmountDisplay} · {limits.nighttime.maxAttempts} tentativas
              </dd>
            </div>
          </dl>
        </div>
        <div className="stack">
          <form className="card form" onSubmit={changeOverdraft}>
            <div className="card__header">
              <h2>Cheque especial</h2>
            </div>
            <p className="muted">
              Limite atual: <strong>{account.overdraftLimitDisplay}</strong>
            </p>
            <label className="field">
              <span>Novo limite (R$)</span>
              <input key={account.overdraftLimit} name="overdraftLimit" type="number" min="0" step="0.01" defaultValue={account.overdraftLimit} required />
            </label>
            <div className="form__actions">
              <button type="submit" className="button button--secondary">
                Salvar limite
              </button>
            </div>
          </form>
          <form className="card form" onSubmit={changeTransferLimits} key={JSON.stringify([limits.daytime, limits.nighttime])}>
            <div className="card__header">
              <h2>Editar limites</h2>
            </div>
            <p className="muted">Valor e tentativas permitidos por hora em cada período. Valem a partir da próxima tentativa.</p>
            <div className="field-row">
              <label className="field">
                <span>Diurno: valor por hora (R$)</span>
                <input name="daytimeAmount" type="number" min="0.01" step="0.01" defaultValue={limits.daytime.maxAmount} required />
              </label>
              <label className="field">
                <span>Diurno: tentativas por hora</span>
                <input name="daytimeAttempts" type="number" min="1" step="1" defaultValue={limits.daytime.maxAttempts} required />
              </label>
            </div>
            <div className="field-row">
              <label className="field">
                <span>Noturno: valor por hora (R$)</span>
                <input name="nighttimeAmount" type="number" min="0.01" step="0.01" defaultValue={limits.nighttime.maxAmount} required />
              </label>
              <label className="field">
                <span>Noturno: tentativas por hora</span>
                <input name="nighttimeAttempts" type="number" min="1" step="1" defaultValue={limits.nighttime.maxAttempts} required />
              </label>
            </div>
            <div className="form__actions">
              <button type="submit" className="button button--secondary">
                Atualizar limites por hora
              </button>
            </div>
          </form>
        </div>
      </section>

      <section className="section">
        <h2 className="section-title">Movimentações</h2>
        <ErrorNotice error={transfers.error} />
        <TransfersTable page={transfers.data} list={list} loading={transfers.loading} />
      </section>
    </>
  )
}
