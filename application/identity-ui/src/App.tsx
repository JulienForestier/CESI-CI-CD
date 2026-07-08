import { LoginForm } from './LoginForm'
import { RegisterForm } from './RegisterForm'

export function App() {
  const isRegister = window.location.pathname.startsWith('/register')
  return isRegister ? <RegisterForm /> : <LoginForm />
}
