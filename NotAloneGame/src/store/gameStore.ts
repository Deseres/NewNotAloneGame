import { create } from 'zustand';
import type { GameSession } from '../types';
import { apiClient } from '../api/client';

interface GameState {
  session: GameSession | null;
  message: string | null;
  isLoading: boolean;
  error: string | null;
  fetchSession: (id: string) => Promise<void>;
  setSessionData: (data: any) => void;
  startGame: () => Promise<void>;
  playLocation: (locationId: number) => Promise<void>;
  creatureTurn: () => Promise<void>;
  nextRound: () => Promise<void>;
  resist: (chosenLocations: number[]) => Promise<void>;
  giveUp: () => Promise<void>;
}

export const useGameStore = create<GameState>((set, get) => ({
  session: null,
  message: null,
  isLoading: false,
  error: null,

  setSessionData: (data: any) => {
    const sessionData = data.session || data;
    const msg = data.message || sessionData.statusMessage || null;
    set({ session: sessionData, message: msg });
  },

  fetchSession: async (id: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.get(`/game/${id}`);
      const sessionData = response.data.session || response.data;
      const msg = response.data.message || sessionData.statusMessage || null;
      set({ session: sessionData, message: msg, isLoading: false });
    } catch (err: any) {
      localStorage.removeItem('sessionId');
      set({ session: null, message: null, isLoading: false, error: null });
    }
  },

  startGame: async () => {
    set({ isLoading: true, error: null, session: null, message: null });
    localStorage.removeItem('sessionId');
    try {
      const response = await apiClient.post('/game/start', {});
      const sessionData = response.data.session;
      const msg = response.data.message || sessionData.statusMessage || null;
      
      if (sessionData && sessionData.id) {
        localStorage.setItem('sessionId', sessionData.id);
        set({ session: sessionData, message: msg, isLoading: false });
      } else {
        throw new Error('Session ID not found');
      }
    } catch (err: any) {
      set({ error: 'Failed to start game', isLoading: false });
    }
  },

  playLocation: async (locationId: number) => {
    const { session, creatureTurn } = get();
    if (!session || !session.id) return;

    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post(`/game/${session.id}/play`, locationId, {
        headers: {
          'Content-Type': 'application/json'
        }
      });
      const sessionData = response.data.session || response.data;
      const msg = response.data.message || sessionData.statusMessage || null;
      set({ session: sessionData, message: msg, isLoading: false });

      setTimeout(() => {
        creatureTurn();
      }, 1000);
    } catch (err: any) {
      set({ error: 'Failed to play location', isLoading: false });
    }
  },

  creatureTurn: async () => {
    const { session } = get();
    if (!session || !session.id) return;

    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post(`/game/${session.id}/creature-turn`, {});
      const sessionData = response.data.session || response.data;
      const msg = response.data.message || sessionData.statusMessage || null;
      set({ session: sessionData, message: msg, isLoading: false });
    } catch (err: any) {
      set({ error: 'Failed to execute creature turn', isLoading: false });
    }
  },

  nextRound: async () => {
    const { session } = get();
    if (!session || !session.id) return;

    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post(`/game/${session.id}/next-round`, {});
      const sessionData = response.data.session || response.data;
      const msg = response.data.message || sessionData.statusMessage || null;
      set({ session: sessionData, message: msg, isLoading: false });
    } catch (err: any) {
      set({ error: 'Failed to advance to next round', isLoading: false });
    }
  },

  resist: async (chosenLocations: number[]) => {
    const { session } = get();
    if (!session || !session.id) return;

    set({ isLoading: true, error: null });
    try {
      const payload = { givenWillpower: 1, chosenLocations };
      const response = await apiClient.post(`/game/${session.id}/resist`, payload, {
        headers: {
          'Content-Type': 'application/json'
        }
      });
      const sessionData = response.data.session || response.data;
      const msg = response.data.message || sessionData.statusMessage || null;
      set({ session: sessionData, message: msg, isLoading: false });
    } catch (err: any) {
      set({ error: 'Failed to resist', isLoading: false });
    }
  },

  giveUp: async () => {
    const { session } = get();
    if (!session || !session.id) return;

    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post(`/game/${session.id}/giveup`, {});
      const sessionData = response.data.session || response.data;
      const msg = response.data.message || sessionData.statusMessage || null;
      set({ session: sessionData, message: msg, isLoading: false });
    } catch (err: any) {
      set({ error: 'Failed to give up', isLoading: false });
    }
  }
}));