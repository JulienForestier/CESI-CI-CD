export function getReturnUrl(): string | null {
  return new URLSearchParams(window.location.search).get('returnUrl')
}

// "//evil.com" est un chemin qui commence par "/" mais que le navigateur interprète comme une
// URL absolue protocol-relative — seul un unique "/" de tête est un chemin local sûr.
function isSafeRelativeUrl(url: string): boolean {
  return url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/\\')
}

// returnUrl vient de la réponse du serveur, elle-même dérivée d'un paramètre de requête
// (donc en dernier ressort contrôlable par l'utilisateur) — on ne redirige que vers un
// chemin relatif pour éviter tout open-redirect, même si le serveur valide déjà returnUrl
// via IIdentityServerInteractionService.IsValidReturnUrl.
export function navigateToReturnUrl(returnUrl: string): void {
  // NOSONAR (tssecurity:S6105) : le scanner ne reconnaît pas isSafeRelativeUrl() comme un
  // sanitizer — la valeur assignée ici est déjà validée ci-dessus (voir returnUrl.test.ts pour
  // la preuve que les URL absolues/protocol-relative retombent sur "/").
  window.location.href = isSafeRelativeUrl(returnUrl) ? returnUrl : '/' // NOSONAR
}
