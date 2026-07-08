export function getReturnUrl(): string | null {
  return new URLSearchParams(window.location.search).get('returnUrl')
}
