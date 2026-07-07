import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import type { AuthUser } from '../context/AuthContext'
import { useAuth } from '../context/AuthContext'
import { useConversations } from '../hooks/useChat'
import { useNotifications } from '../hooks/useNotifications'

export function Header() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const conversationsQuery = useConversations()
  const unreadMessagesCount = conversationsQuery.data?.filter((c) => c.hasUnread).length ?? 0
  const notificationsQuery = useNotifications()
  const unreadNotificationsCount = notificationsQuery.data?.filter((n) => !n.isRead).length ?? 0
  const [isMenuOpen, setIsMenuOpen] = useState(false)

  function closeMenu() {
    setIsMenuOpen(false)
  }

  function handleLogout() {
    logout()
    closeMenu()
    navigate('/')
  }

  return (
    <header className="border-b border-ink/10 bg-card">
      <div className="mx-auto flex max-w-5xl items-center gap-4 px-6 py-4">
        <Link to="/" className="flex shrink-0 items-center gap-2.5" onClick={closeMenu}>
          <span className="flex h-[38px] w-[38px] items-center justify-center rounded-full border-2 border-ink font-display text-[20px] leading-none">
            C
          </span>
          <span className="font-display text-[23px] tracking-[-0.01em]">
            Collector<span className="text-burnt">.shop</span>
          </span>
        </Link>

        <PrimaryNav user={user} orientation="horizontal" className="hidden flex-1 lg:flex" />

        <div className="hidden shrink-0 items-center gap-3 lg:flex">
          <DesktopUtilityIcons
            user={user}
            unreadMessagesCount={unreadMessagesCount}
            unreadNotificationsCount={unreadNotificationsCount}
          />
          <AuthActions user={user} onLogout={handleLogout} />
        </div>

        <button
          type="button"
          aria-label={isMenuOpen ? 'Fermer le menu' : 'Ouvrir le menu'}
          aria-expanded={isMenuOpen}
          onClick={() => setIsMenuOpen((open) => !open)}
          className="ml-auto flex h-9 w-9 shrink-0 items-center justify-center rounded-full border-[1.5px] border-ink text-base lg:hidden"
        >
          {isMenuOpen ? '✕' : '☰'}
        </button>
      </div>

      {isMenuOpen && (
        <div className="border-t border-ink/10 bg-card px-6 py-4 lg:hidden">
          <PrimaryNav user={user} orientation="vertical" onNavigate={closeMenu} className="flex flex-col" />
          <MobileUtilityLinks
            user={user}
            unreadMessagesCount={unreadMessagesCount}
            unreadNotificationsCount={unreadNotificationsCount}
            onNavigate={closeMenu}
          />
          <div className="mt-3 flex flex-col gap-2 border-t border-ink/10 pt-3">
            <AuthActions user={user} onLogout={handleLogout} orientation="vertical" onNavigate={closeMenu} />
          </div>
        </div>
      )}
    </header>
  )
}

interface NavGroupProps {
  user: AuthUser | null
  orientation: 'horizontal' | 'vertical'
  onNavigate?: () => void
  className?: string
}

function PrimaryNav({ user, orientation, onNavigate, className = '' }: NavGroupProps) {
  const isVertical = orientation === 'vertical'
  const linkClass = isVertical
    ? 'rounded-lg px-3 py-2.5 font-medium text-ink hover:bg-surface'
    : 'font-medium text-ink hover:text-burnt'

  return (
    <nav
      className={`${isVertical ? 'flex flex-col' : 'items-center justify-center gap-7 text-[15px]'} font-ui ${className}`}
    >
      <Link to="/" className={linkClass} onClick={onNavigate}>
        Catalogue
      </Link>
      {user && (
        <>
          <Link to="/favoris" className={linkClass} onClick={onNavigate}>
            Mes favoris
          </Link>
          <Link to="/mes-annonces" className={linkClass} onClick={onNavigate}>
            Mes annonces
          </Link>
        </>
      )}
    </nav>
  )
}

function IconLink({
  to,
  icon,
  label,
  badge,
}: {
  to: string
  icon: string
  label: string
  badge: number
}) {
  return (
    <Link
      to={to}
      aria-label={label}
      title={label}
      className="relative flex h-9 w-9 items-center justify-center rounded-full border-[1.5px] border-ink text-base hover:bg-surface"
    >
      <span aria-hidden="true">{icon}</span>
      {badge > 0 && (
        <span className="absolute -top-1.5 -right-1.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-burnt px-1 text-[10px] font-bold text-surface">
          {badge}
        </span>
      )}
    </Link>
  )
}

function DesktopUtilityIcons({
  user,
  unreadMessagesCount,
  unreadNotificationsCount,
}: {
  user: AuthUser | null
  unreadMessagesCount: number
  unreadNotificationsCount: number
}) {
  if (!user) {
    return null
  }

  return (
    <div className="flex items-center gap-2">
      <IconLink to="/messages" icon="💬" label="Messages" badge={unreadMessagesCount} />
      <IconLink to="/notifications" icon="🔔" label="Notifications" badge={unreadNotificationsCount} />
      <IconLink to="/centres-interet" icon="🎯" label="Centres d'intérêt" badge={0} />
      {user.isAdmin && <IconLink to="/admin/moderation" icon="🛡️" label="Modération" badge={0} />}
    </div>
  )
}

function MobileUtilityLinks({
  user,
  unreadMessagesCount,
  unreadNotificationsCount,
  onNavigate,
}: {
  user: AuthUser | null
  unreadMessagesCount: number
  unreadNotificationsCount: number
  onNavigate?: () => void
}) {
  if (!user) {
    return null
  }

  const linkClass = 'flex items-center justify-between rounded-lg px-3 py-2.5 font-medium text-ink hover:bg-surface'
  const badgeClass = 'flex h-5 min-w-5 items-center justify-center rounded-full bg-burnt px-1.5 text-[11px] font-bold text-surface'

  return (
    <nav className="mt-1 flex flex-col border-t border-ink/10 pt-1 font-ui">
      <Link to="/messages" className={linkClass} onClick={onNavigate}>
        <span>💬 Messages</span>
        {unreadMessagesCount > 0 && <span className={badgeClass}>{unreadMessagesCount}</span>}
      </Link>
      <Link to="/notifications" className={linkClass} onClick={onNavigate}>
        <span>🔔 Notifications</span>
        {unreadNotificationsCount > 0 && <span className={badgeClass}>{unreadNotificationsCount}</span>}
      </Link>
      <Link to="/centres-interet" className={linkClass} onClick={onNavigate}>
        <span>🎯 Centres d'intérêt</span>
      </Link>
      {user.isAdmin && (
        <Link to="/admin/moderation" className={linkClass} onClick={onNavigate}>
          <span>🛡️ Modération</span>
        </Link>
      )}
    </nav>
  )
}

function AuthActions({
  user,
  onLogout,
  orientation = 'horizontal',
  onNavigate,
}: {
  user: AuthUser | null
  onLogout: () => void
  orientation?: 'horizontal' | 'vertical'
  onNavigate?: () => void
}) {
  const isVertical = orientation === 'vertical'

  if (!user) {
    return (
      <div className={isVertical ? 'flex flex-col gap-2' : 'flex items-center gap-4 font-ui text-[15px]'}>
        <Link
          to="/connexion"
          className={isVertical ? 'rounded-lg px-3 py-2.5 font-medium text-ink hover:bg-surface' : 'font-medium text-ink hover:text-burnt'}
          onClick={onNavigate}
        >
          Connexion
        </Link>
        <Link
          to="/inscription"
          className="rounded-full bg-ink px-5 py-2.5 text-center font-ui text-[13px] font-semibold text-card"
          onClick={onNavigate}
        >
          Vendre un objet
        </Link>
      </div>
    )
  }

  return (
    <div className={isVertical ? 'flex flex-col gap-2' : 'flex items-center gap-3 font-ui text-sm'}>
      <Link
        to="/annonces/nouvelle"
        className="rounded-full bg-ink px-5 py-2.5 text-center font-ui text-[13px] font-semibold text-card"
        onClick={onNavigate}
      >
        Vendre un objet
      </Link>
      {isVertical ? (
        <span className="px-3 text-brown-2">{user.displayName}</span>
      ) : (
        <span
          title={user.displayName}
          aria-label={user.displayName}
          className="flex h-9 w-9 items-center justify-center rounded-full border-[1.5px] border-ink font-display text-sm"
        >
          {user.displayName.charAt(0).toUpperCase()}
        </span>
      )}
      <button
        type="button"
        onClick={onLogout}
        className="rounded-full border-[1.5px] border-ink px-4 py-2 text-xs font-semibold text-ink hover:bg-surface"
      >
        Se déconnecter
      </button>
    </div>
  )
}
