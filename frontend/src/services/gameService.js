import apiClient from '../api/apiClient';

// Получить все игровые сессии пользователя
export const getGameSessions = async () => {
  try {
    const response = await apiClient.get('/game/sessions');
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Создать новую игровую сессию
export const createGameSession = async (gameData) => {
  try {
    const response = await apiClient.post('/game/sessions', gameData);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Получить детали конкретной сессии
export const getGameSession = async (sessionId) => {
  try {
    const response = await apiClient.get(`/game/sessions/${sessionId}`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Обновить статус игровой сессии
export const updateGameSession = async (sessionId, updateData) => {
  try {
    const response = await apiClient.put(`/game/sessions/${sessionId}`, updateData);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Завершить игровую сессию
export const endGameSession = async (sessionId) => {
  try {
    const response = await apiClient.post(`/game/sessions/${sessionId}/end`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Получить историю игр
export const getGameHistory = async (userId) => {
  try {
    const response = await apiClient.get(`/game/history/${userId}`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};
