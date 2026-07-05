import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import App from '../App.tsx'

describe('App Component', () => {
    it('should render the app', () => {
        render(<App />)
        expect(screen.getAllByRole('heading'))
    })
})