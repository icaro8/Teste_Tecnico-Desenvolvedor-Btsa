import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Icon } from './Icon'

interface PageHeaderProps {
  title: string
  subtitle?: ReactNode
  actions?: ReactNode
  back?: { to: string; label: string }
  leading?: ReactNode
}

export function PageHeader({ title, subtitle, actions, back, leading }: PageHeaderProps) {
  return (
    <div className="page-header">
      {back && (
        <Link to={back.to} className="back-link">
          <Icon name="arrowLeft" size={16} />
          {back.label}
        </Link>
      )}
      <div className="page-header__row">
        <div className="page-header__title">
          {leading}
          <div>
            <h1>{title}</h1>
            {subtitle && <div className="page-header__subtitle">{subtitle}</div>}
          </div>
        </div>
        {actions && <div className="page-header__actions">{actions}</div>}
      </div>
    </div>
  )
}
