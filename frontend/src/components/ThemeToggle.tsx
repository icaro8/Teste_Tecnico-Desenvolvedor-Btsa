import { useTheme, type ThemePreference } from '../hooks/useTheme'
import { Icon, type IconName } from './Icon'

const OPTIONS: { value: ThemePreference; label: string; icon: IconName }[] = [
  { value: 'system', label: 'Sistema', icon: 'monitor' },
  { value: 'light', label: 'Claro', icon: 'sun' },
  { value: 'dark', label: 'Escuro', icon: 'moon' }
]

/** Controle segmentado usado na barra lateral. */
export function ThemeSwitcher() {
  const { preference, setPreference } = useTheme()
  return (
    <div className="segmented" role="group" aria-label="Tema">
      {OPTIONS.map((option) => (
        <button
          key={option.value}
          type="button"
          className="segmented__option"
          aria-pressed={preference === option.value}
          title={`Tema: ${option.label.toLowerCase()}`}
          onClick={() => setPreference(option.value)}
        >
          <Icon name={option.icon} size={16} />
          <span>{option.label}</span>
        </button>
      ))}
    </div>
  )
}

/** Botão compacto que alterna entre os temas, usado na barra superior do celular. */
export function ThemeToggle() {
  const { preference, setPreference } = useTheme()
  const index = OPTIONS.findIndex((option) => option.value === preference)
  const currentOption = OPTIONS[index]
  const next = OPTIONS[(index + 1) % OPTIONS.length]
  const label = `Tema: ${currentOption.label.toLowerCase()} (alternar para ${next.label.toLowerCase()})`

  return (
    <button type="button" className="icon-button" onClick={() => setPreference(next.value)} aria-label={label} title={label}>
      <Icon name={currentOption.icon} />
    </button>
  )
}
