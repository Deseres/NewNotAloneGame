import { create } from 'zustand';
import { apiClient } from '../api/client';

interface AuthState {
  token: string | null;
  isLoading: boolean;
  error: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: localStorage.getItem('token'), 
  isLoading: false,
  error: null,

  login: async (email, password) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post('/auth/login', { email, password });
      const token = response.data.token || 'temp-token'; 
      localStorage.setItem('token', token);
      set({ token, isLoading: false });
    } catch (err: any) {
      set({ 
        error: err.response?.data?.message || 'Login failed. Check your credentials.', 
        isLoading: false 
      });
      throw err; // Прокидываем ошибку, чтобы отловить её в компоненте
    }
  },

  register: async (email, password) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post('/auth/register', { email, password });
      const token = response.data.token || 'temp-token';
      localStorage.setItem('token', token);
      set({ token, isLoading: false });
    } catch (err: any) {
      set({ 
        error: err.response?.data?.message || 'Registration failed.', 
        isLoading: false 
      });
      throw err;
    }
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('sessionId');
    set({ token: null, error: null });
  },
}));