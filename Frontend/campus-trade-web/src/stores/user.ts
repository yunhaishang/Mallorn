import { defineStore } from 'pinia'
import { ref } from 'vue'
import { authApi, type LoginData, type RegisterData } from '@/services/api'

export interface User {
  userId?: number
  username: string
  email: string
  fullName?: string
  phone?: string
  studentId?: string
}

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(null)
  const token = ref<string>('')
  const isLoggedIn = ref<boolean>(false)

  // 初始化用户状态
  const initializeAuth = () => {
    const savedToken = localStorage.getItem('token')
    const savedUser = localStorage.getItem('user')

    if (savedToken && savedUser) {
      token.value = savedToken
      user.value = JSON.parse(savedUser)
      isLoggedIn.value = true
    }
  }

  // 登录
  const login = async (loginData: LoginData) => {
    try {
      const response = await authApi.login(loginData)

      if (response.success) {
        token.value = response.data.token
        user.value = {
          username: response.data.username,
          email: response.data.email,
          fullName: response.data.fullName,
        }
        isLoggedIn.value = true

        // 保存到本地存储
        localStorage.setItem('token', token.value)
        localStorage.setItem('user', JSON.stringify(user.value))

        return { success: true, message: response.message }
      }

      return { success: false, message: response.message }
    } catch (error: any) {
      const message = error.response?.data?.message || '登录失败，请重试'
      return { success: false, message }
    }
  }

  // 注册
  const register = async (registerData: RegisterData) => {
    try {
      const response = await authApi.register(registerData)

      if (response.success) {
        return { success: true, message: response.message }
      }

      return { success: false, message: response.message }
    } catch (error: any) {
      const message = error.response?.data?.message || '注册失败，请重试'
      return { success: false, message }
    }
  }

  // 登出
  const logout = () => {
    user.value = null
    token.value = ''
    isLoggedIn.value = false

    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  // 获取用户信息
  const fetchUserInfo = async (username: string) => {
    try {
      const response: any = await authApi.getUser(username)
      if (response.success) {
        user.value = response.data
        localStorage.setItem('user', JSON.stringify(user.value))
      }
      return response
    } catch (error: any) {
      console.error('获取用户信息失败:', error)
      return { success: false, message: '获取用户信息失败' }
    }
  }

  return {
    user,
    token,
    isLoggedIn,
    initializeAuth,
    login,
    register,
    logout,
    fetchUserInfo,
  }
})
