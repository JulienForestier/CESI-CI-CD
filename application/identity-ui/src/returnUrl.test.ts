import { beforeEach, describe, expect, it } from 'vitest'
import { getReturnUrl, navigateToReturnUrl } from './returnUrl'

describe('getReturnUrl', () => {
  beforeEach(() => {
    // @ts-expect-error test-only override of a read-only global
    delete window.location
    // @ts-expect-error test-only override of a read-only global
    window.location = { href: '', search: '' }
  })

  it('reads the returnUrl query parameter', () => {
    window.location.search = '?returnUrl=%2Fconnect%2Fauthorize%2Fcallback'
    expect(getReturnUrl()).toBe('/connect/authorize/callback')
  })

  it('returns null when there is no returnUrl parameter', () => {
    window.location.search = ''
    expect(getReturnUrl()).toBeNull()
  })
})

describe('navigateToReturnUrl', () => {
  beforeEach(() => {
    // @ts-expect-error test-only override of a read-only global
    delete window.location
    // @ts-expect-error test-only override of a read-only global
    window.location = { href: '', search: '' }
  })

  it('navigates to a safe relative path', () => {
    navigateToReturnUrl('/connect/authorize/callback?request_uri=abc')
    expect(window.location.href).toBe('/connect/authorize/callback?request_uri=abc')
  })

  it('falls back to / for a protocol-relative URL (open-redirect attempt)', () => {
    navigateToReturnUrl('//evil.example.com')
    expect(window.location.href).toBe('/')
  })

  it('falls back to / for an absolute URL', () => {
    navigateToReturnUrl('https://evil.example.com')
    expect(window.location.href).toBe('/')
  })

  it('falls back to / for a backslash-prefixed URL', () => {
    navigateToReturnUrl('/\\evil.example.com')
    expect(window.location.href).toBe('/')
  })
})
