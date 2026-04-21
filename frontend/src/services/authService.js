import apiClient from '../api/apiClient';

// Регистрация пользователя
export const registerUser = async (username, email, password) => {
  try {
    const response = await apiClient.post('/auth/register', {
      username,
      email,
      password,
    });
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Вход пользователя
export const loginUser = async (username, password) => {
  try {
    const response = await apiClient.post('/auth/login', {
      username,
      password,
    });
    if (response.data.token) {
      localStorage.setItem('token', response.data.token);
      localStorage.setItem('user', JSON.stringify(response.data.user));
    }
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Выход пользователя
export const logoutUser = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('user');
};

// Получение профиля текущего пользователя
export const getCurrentUser = async () => {
  try {
    const response = await apiClient.get('/auth/profile');
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Обновление профиля пользователя
export const updateUserProfile = async (userData) => {
  try {
    const response = await apiClient.put('/auth/profile', userData);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};
