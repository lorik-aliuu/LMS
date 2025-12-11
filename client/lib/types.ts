export interface User {
  userId: string
  userName: string
  email: string
  role: "User" | "Admin"
  firstName: string
  lastName: string
  fullName: string
}

export interface AuthResponse {
  success: boolean
  message: string
  data: {
    token: string
    refreshToken: string
    userId: string
    userName: string
    email: string
    role: "User" | "Admin"
    firstName: string
    lastName: string
    fullName: string
    expiresAt: string
  }
}

export interface RefreshTokenResponse {
  success: boolean
  message: string
  statusCode?: number
  data: {
    token: string
    refreshToken: string
    expiresAt: string
    userId: string
    userName: string
    email: string
    role: "User" | "Admin"
    firstName: string
    lastName: string
    fullName: string
  }
}

export enum ReadingStatus {
  NotStarted = 0,
  Reading = 1,
  Completed = 2,
}

export interface Book {
  id: number
  title: string
  author: string
  genre: string
  price: number
  readingStatus: ReadingStatus
  publicationYear: number
  rating?: number
  coverImageUrl?: string
}

export interface CreateBookDTO {
  title: string
  author: string
  genre: string
  price: number
  readingStatus: ReadingStatus
  publicationYear: number
  rating?: number
  coverImageUrl?: string
}

export interface UpdateBookDTO {
  title?: string
  author?: string
  genre?: string
  price?: number
  readingStatus?: ReadingStatus
  publicationYear: number
  rating?: number
  coverImageUrl?: string
}

export interface UserProfile {
  userId: string
  userName: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  phoneNumber?: string | null
  dateOfBirth?: string | null
  createdAt: string
  role: "User" | "Admin"
  totalBooks: number
}

export interface UpdateProfileDTO {
  firstName?: string
  lastName?: string
  email?: string
  phoneNumber?: string
  dateOfBirth?: string
}

export interface ChangePasswordDTO {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

export interface DeleteAccountDTO {
  password: string
}


export interface AdminUser {
  userId: string
  userName: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  phoneNumber?: string | null
  dateOfBirth?: string | null
  createdAt: string
  role: "User" | "Admin"
  totalBooks: number
}

export interface UpdateRoleDTO {
  role: "User" | "Admin"
}

export interface AdminBook extends Book {
  userId?: string
  userName?: string
  ownerName?: string
}

export interface AiQueryRequest {
  query: string
}

export interface AiQueryResponse {
  success: boolean
  answer: string
  interpretedQuery?: string
  data?: Record<string, unknown>[]
  chartType?: "bar" | "table" | string
  errorMessage?: string
  timestamp: string
}

export interface AiExamplesResponse {
  success: boolean
  data: string[]
}

export interface ChatMessage {
  id: string
  role: "user" | "assistant"
  content: string
  data?: Record<string, unknown>[]
  chartType?: string
  timestamp: Date
}


