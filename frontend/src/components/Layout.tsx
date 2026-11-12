import { Link, NavLink, Outlet } from 'react-router'
import { Icon } from './Icon'
import { ThemeSwitcher, ThemeToggle } from './ThemeToggle'

function Brand() {
  return (
    <Link to="/" className="brand">
      <img src="/favicon.svg" alt="" width={32} height={32} />
      <span>Transferências</span>
    </Link>
  )
}

export function Layout() {
  return (
    <div className="app">
      <aside className="sidebar">
        <Brand />
        <nav className="sidenav" aria-label="Principal">
          <span className="sidenav__label">Menu</span>
          <NavLink to="/" end>
            <Icon name="accounts" />
            Contas
          </NavLink>
          <NavLink to="/transferencias" end>
            <Icon name="transfers" />
            Transferências
          </NavLink>
        </nav>
        <Link to="/transferencias/nova" className="button button--primary button--block">
          <Icon name="send" size={16} />
          Nova transferência
        </Link>
        <div className="sidebar__footer">
          <span className="sidenav__label">Aparência</span>
          <ThemeSwitcher />
        </div>
      </aside>

      <header className="mobilebar">
        <Brand />
        <ThemeToggle />
      </header>

      <main className="main">
        <div className="main__inner">
          <Outlet />
        </div>
      </main>

      <nav className="tabbar" aria-label="Navegação móvel">
        <NavLink to="/" end>
          <Icon name="accounts" size={20} />
          <span>Contas</span>
        </NavLink>
        <Link to="/transferencias/nova" className="tabbar__cta" aria-label="Nova transferência">
          <Icon name="plus" size={24} />
        </Link>
        <NavLink to="/transferencias" end>
          <Icon name="transfers" size={20} />
          <span>Transferências</span>
        </NavLink>
      </nav>
    </div>
  )
}
