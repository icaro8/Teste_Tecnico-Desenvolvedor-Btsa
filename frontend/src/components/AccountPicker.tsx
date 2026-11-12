import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import { api } from '../api/client'
import type { Account } from '../api/types'
import { useAsync } from '../hooks/useAsync'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { Badge } from './Badge'

interface AccountPickerProps {
  name: string
  label: string
  initialAccountId?: string
  detail: (account: Account) => string
  onChange?: (account: Account) => void
}

export function AccountPicker({ name, label, initialAccountId = '', detail, onChange }: AccountPickerProps) {
  const id = useId()
  const labelId = `${id}-label`
  const listId = `${id}-list`
  const rootRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<Account | null>(null)
  const [activeIndex, setActiveIndex] = useState(0)
  const term = useDebouncedValue(search)

  const results = useAsync(
    () => (open ? api.listAccounts({ search: term, sortBy: 'HolderName', sortDirection: 'Asc', pageSize: 20 }) : Promise.resolve(null)),
    [term, open]
  )
  const preset = useAsync(() => (initialAccountId ? api.getAccount(initialAccountId) : Promise.resolve(null)), [initialAccountId])

  useEffect(() => {
    if (preset.data && !selected) setSelected(preset.data.account)
  }, [preset.data])

  useEffect(() => {
    if (!open) return
    searchRef.current?.focus()
    const closeOnOutsideClick = (event: PointerEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false)
    }
    document.addEventListener('pointerdown', closeOnOutsideClick)
    return () => document.removeEventListener('pointerdown', closeOnOutsideClick)
  }, [open])

  useEffect(() => setActiveIndex(0), [results.data])

  const options = results.data?.items ?? []

  const choose = (account: Account) => {
    setSelected(account)
    setOpen(false)
    setSearch('')
    onChange?.(account)
    triggerRef.current?.focus()
  }

  const close = () => {
    setOpen(false)
    triggerRef.current?.focus()
  }

  const onSearchKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setActiveIndex((index) => Math.min(index + 1, options.length - 1))
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setActiveIndex((index) => Math.max(index - 1, 0))
    } else if (event.key === 'Enter') {
      event.preventDefault()
      if (options[activeIndex]) choose(options[activeIndex])
    } else if (event.key === 'Escape') {
      event.preventDefault()
      close()
    }
  }

  const onTriggerKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      setOpen(true)
    }
  }

  const status = results.loading ? 'Carregando…' : options.length === 0 ? 'Nenhuma conta encontrada' : null

  return (
    <div className="field picker" ref={rootRef}>
      <span id={labelId}>{label}</span>
      <button
        ref={triggerRef}
        type="button"
        role="combobox"
        className={`picker__trigger${open ? ' picker__trigger--open' : ''}`}
        aria-labelledby={labelId}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={listId}
        onClick={() => setOpen((current) => !current)}
        onKeyDown={onTriggerKeyDown}
      >
        {selected ? (
          <span className="picker__value">
            <span className="picker__title">
              <span className="picker__name">{selected.holderName}</span>
              <Badge tone={selected.statusTone} label={selected.statusLabel} />
            </span>
            <span className="picker__detail">{detail(selected)}</span>
          </span>
        ) : (
          <span className="picker__placeholder">Selecione uma conta</span>
        )}
        <svg className="picker__chevron" viewBox="0 0 20 20" aria-hidden="true">
          <path d="M5 7.5l5 5 5-5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>
      <input
        className="picker__validator"
        name={name}
        value={selected?.id ?? ''}
        required
        tabIndex={-1}
        aria-hidden="true"
        onChange={() => undefined}
        onFocus={() => triggerRef.current?.focus()}
      />
      {open && (
        <div className="picker__popover">
          <input
            ref={searchRef}
            type="search"
            className="picker__search"
            aria-label={`Filtrar ${label.toLowerCase()} por CPF`}
            aria-controls={listId}
            aria-activedescendant={options[activeIndex] ? `${id}-option-${activeIndex}` : undefined}
            placeholder="Filtrar por CPF"
            inputMode="numeric"
            autoComplete="off"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            onKeyDown={onSearchKeyDown}
          />
          <ul id={listId} role="listbox" aria-labelledby={labelId} className="picker__list">
            {status ? (
              <li className="picker__status">{status}</li>
            ) : (
              options.map((account, index) => (
                <li
                  key={account.id}
                  id={`${id}-option-${index}`}
                  role="option"
                  aria-selected={account.id === selected?.id}
                  className={`picker__option${index === activeIndex ? ' picker__option--active' : ''}`}
                  onPointerEnter={() => setActiveIndex(index)}
                  onClick={() => choose(account)}
                >
                  <span className="picker__title">
                    <span className="picker__name">{account.holderName}</span>
                    <Badge tone={account.statusTone} label={account.statusLabel} />
                  </span>
                  <span className="picker__detail">{detail(account)}</span>
                </li>
              ))
            )}
          </ul>
        </div>
      )}
    </div>
  )
}
