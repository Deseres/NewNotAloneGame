import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import * as authService from '../services/authService';

const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Проверка пользователя при загрузке
  useEffect(() => {
    const checkUser = () => {
      const token = localStorage.getItem('token');
      const storedUser = localStorage.getItem('user');
      
      if (token && storedUser) {
        try {
          setUser(JSON.parse(storedUser));
        } catch (err) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
        }
      }
      setLoading(false);
    };

    checkUser();
  }, []);

  const login = useCallback(async (username, password) => {
    try {
      setError(null);
      const response = await authService.loginUser(username, password);
      setUser(response.user);
      return response;
    } catch (err) {
      setError(err.message || 'Ошибка при входе');
      throw err;
    }
  }, []);

  const register = useCallback(async (username, email, password) => {
    try {
      setError(null);
      const response = await authService.registerUser(username, email, password);
      return response;
    } catch (err) {
      setError(err.message || 'Ошибка при регистрации');
      throw err;
    }
  }, []);

  const logout = useCallback(() => {
    authService.logoutUser();
    setUser(null);
    setError(null);
  }, []);

  const value = {
    user,
    loading,
    error,
    login,
    register,
    logout,
    isAuthenticated: !!user,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth должен использоваться внутри AuthProvider');
  }
  return context;
};
