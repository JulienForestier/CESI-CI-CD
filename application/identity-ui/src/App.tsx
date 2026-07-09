import { Footer } from './Footer'
import { Header } from './Header'
import { LoginForm } from './LoginForm'
import { RegisterForm } from './RegisterForm'

export function App() {
  const isRegister = window.location.pathname.startsWith('/register')

  return (
    <div className="flex min-h-screen flex-col bg-card font-body text-ink">
      <Header />
      <main className="mx-auto w-full max-w-5xl flex-1 px-6 py-10">
        {isRegister ? <RegisterForm /> : <LoginForm />}
      </main>
      <Footer />
    </div>
  )
}
