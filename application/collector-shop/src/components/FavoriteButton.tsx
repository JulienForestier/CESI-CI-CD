import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useFavoriteIds, useToggleFavorite } from '../hooks/useFavorites'

interface FavoriteButtonProps {
  listingId: string
  className?: string
  variant?: 'icon' | 'button'
}

export function FavoriteButton({ listingId, className = '', variant = 'icon' }: FavoriteButtonProps) {
  const { user } = useAuth()
  const navigate = useNavigate()
  const favoriteIdsQuery = useFavoriteIds()
  const toggleFavorite = useToggleFavorite()

  const isFavorited = favoriteIdsQuery.data?.includes(listingId) ?? false

  function handleClick() {
    if (!user) {
      navigate('/connexion')
      return
    }
    toggleFavorite.mutate({ listingId, isFavorited })
  }

  if (variant === 'button') {
    return (
      <button
        type="button"
        aria-pressed={isFavorited}
        onClick={handleClick}
        disabled={toggleFavorite.isPending}
        className={`rounded-xl border-[1.5px] border-ink bg-surface py-3.5 font-ui text-sm font-semibold text-ink disabled:opacity-50 ${className}`}
      >
        {isFavorited ? '♥ Retirer des favoris' : '♡ Ajouter aux favoris'}
      </button>
    )
  }

  return (
    <button
      type="button"
      aria-label={isFavorited ? 'Retirer des favoris' : 'Ajouter aux favoris'}
      aria-pressed={isFavorited}
      onClick={handleClick}
      disabled={toggleFavorite.isPending}
      className={`flex h-8 w-8 items-center justify-center rounded-full border-[1.5px] border-ink bg-surface text-base leading-none disabled:opacity-50 ${className}`}
    >
      <span className={isFavorited ? 'text-burnt' : 'text-ink'}>{isFavorited ? '♥' : '♡'}</span>
    </button>
  )
}
