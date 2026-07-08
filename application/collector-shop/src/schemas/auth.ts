import { z } from 'zod'

export const loginSchema = z.object({
  email: z.string().trim().min(1, "L'email est requis").email('Email invalide'),
  password: z.string().min(1, 'Le mot de passe est requis'),
})

export type LoginFormValues = z.infer<typeof loginSchema>

export const registerSchema = z.object({
  displayName: z.string().trim().min(1, 'Le pseudo est requis'),
  email: z.string().trim().min(1, "L'email est requis").email('Email invalide'),
  password: z.string().min(8, 'Le mot de passe doit contenir au moins 8 caractères'),
})

export type RegisterFormValues = z.infer<typeof registerSchema>
