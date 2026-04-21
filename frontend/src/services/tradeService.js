import apiClient from '../api/apiClient';

// Получить доступные товары
export const getAvailableItems = async () => {
  try {
    const response = await apiClient.get('/trade/items');
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Получить рыночные предложения
export const getMarketOffers = async () => {
  try {
    const response = await apiClient.get('/trade/offers');
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Создать торговое предложение
export const createTradeOffer = async (offerData) => {
  try {
    const response = await apiClient.post('/trade/offers', offerData);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Принять торговое предложение
export const acceptTradeOffer = async (offerId) => {
  try {
    const response = await apiClient.post(`/trade/offers/${offerId}/accept`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Отклонить торговое предложение
export const rejectTradeOffer = async (offerId) => {
  try {
    const response = await apiClient.post(`/trade/offers/${offerId}/reject`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Получить историю торговли пользователя
export const getTradeHistory = async (userId) => {
  try {
    const response = await apiClient.get(`/trade/history/${userId}`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};

// Получить инвентарь пользователя
export const getUserInventory = async (userId) => {
  try {
    const response = await apiClient.get(`/trade/inventory/${userId}`);
    return response.data;
  } catch (error) {
    throw error.response?.data || error.message;
  }
};
