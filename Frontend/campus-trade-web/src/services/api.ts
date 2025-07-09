import axios, { type InternalAxiosRequestConfig, type AxiosResponse, type AxiosError } from 'axios'

// 创建 axios 实例
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// 请求拦截器
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error: AxiosError) => {
    return Promise.reject(error)
  },
)

// 响应拦截器
api.interceptors.response.use(
  (response: AxiosResponse) => {
    return response.data
  },
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      // 清除 token 并跳转到登录页
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  },
)

// 登录接口
export interface LoginData {
  username: string
  password: string
}

export interface RegisterData {
  username: string
  email: string
  password: string
  fullName?: string
  phone?: string
  studentId?: string
}

export interface LoginResponse {
  success: boolean
  message: string
  data: {
    token: string
    username: string
    email: string
    fullName?: string
  }
}

export interface RegisterResponse {
  success: boolean
  message: string
  data: {
    userId: number
    username: string
    email: string
    fullName?: string
  }
}

// API 方法
export const authApi = {
  // 登录
  login: (data: LoginData): Promise<LoginResponse> => {
    return api.post('/api/auth/login', data)
  },

  // 注册
  register: (data: RegisterData): Promise<RegisterResponse> => {
    return api.post('/api/auth/register', data)
  },

  // 获取用户信息
  getUser: (username: string) => {
    return api.get(`/api/auth/user/${username}`)
  },
}

export default api
