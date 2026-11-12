import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { api } from '../api/client'
import { ErrorNotice } from '../components/Notice'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'

export function NewAccountPage() {
  const navigate = useNavigate()
  const [error, setError] = useState<Error | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setSubmitting(true)
    setError(null)
    try {
      const account = await api.openAccount({
        holderName: String(form.get('holderName')),
        holderDocument: String(form.get('holderDocument')),
        initialBalance: Number(form.get('initialBalance')),
        overdraftLimit: Number(form.get('overdraftLimit'))
      })
      navigate(`/contas/${account.id}`)
    } catch (reason) {
      setError(reason as Error)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      <PageHeader
        back={{ to: '/', label: 'Contas' }}
        title="Nova conta"
        subtitle="Informe o titular, o saldo inicial e o limite de cheque especial."
      />
      <div className="form-layout">
      <form className="card form" onSubmit={submit}>
        <ErrorNotice error={error} />
        <label className="field">
          <span>Nome do titular</span>
          <input name="holderName" required maxLength={150} autoComplete="name" />
        </label>
        <label className="field">
          <span>CPF</span>
          <input name="holderDocument" required inputMode="numeric" placeholder="000.000.000-00" maxLength={14} />
        </label>
        <div className="field-row">
          <label className="field">
            <span>Saldo inicial (R$)</span>
            <input name="initialBalance" type="number" min="0" step="0.01" defaultValue="0" required />
          </label>
          <label className="field">
            <span>Limite de cheque especial (R$)</span>
            <input name="overdraftLimit" type="number" min="0" step="0.01" defaultValue="0" required />
          </label>
        </div>
        <div className="form__actions">
          <button type="button" className="button button--ghost" onClick={() => navigate(-1)}>
            Voltar
          </button>
          <button type="submit" className="button button--primary" disabled={submitting}>
            {submitting ? 'Abrindo…' : 'Abrir conta'}
          </button>
        </div>
      </form>
      <aside className="card card--muted tips">
        <h2>
          <Icon name="info" size={18} />
          Antes de abrir
        </h2>
        <ul>
          <li>O CPF precisa ser válido; pontos e traço são opcionais.</li>
          <li>O valor disponível para transferir é o saldo somado ao cheque especial.</li>
          <li>O limite de cheque especial pode ser alterado depois, na página da conta.</li>
        </ul>
      </aside>
      </div>
    </>
  )
}
