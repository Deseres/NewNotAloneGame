import apiClient from '../api/apiClient';

// Получить доступные карты выживания
export const getSurvivalCards = async () => {
  try {
    const response = await apiClient.get('/survival/cards');
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Получить деталь карты выживания
export const getSurvivalCard = async (cardId) => {
  try {
    const response = await apiClient.get(`/survival/cards/${cardId}`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Сыграть карту
export const playCard = async (sessionId, cardData) => {
  try {
    const response = await apiClient.post(`/survival/play`, {
      sessionId,
      ...cardData,
    });
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Совершить сопротивление
export const resistAction = async (sessionId, resistData) => {
  try {
    const response = await apiClient.post(`/survival/resist`, {
      sessionId,
      ...resistData,
    });
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Получить статус выживания в текущей сессии
export const getSurvivalStatus = async (sessionId) => {
  try {
    const response = await apiClient.get(`/survival/status/${sessionId}`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Получить доступные действия выживания
export const getAvailableActions = async (sessionId) => {
  try {
    const response = await apiClient.get(`/survival/actions/${sessionId}`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};
