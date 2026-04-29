import { create } from 'zustand';
import { apiClient } from '../api/client';
import type { SurvivalCard } from '../types';

export const CARDS_DICTIONARY: Record<number, SurvivalCard> = {
  1: { id: 1, name: 'Heal', type: 'Heal', phase: 'Selection', description: 'Restore 1 willpower (max 3)' },
  2: { id: 2, name: 'Beacon', type: 'Beacon', phase: 'Selection', description: 'Light the beacon. Useful at Beach.' },
  3: { id: 3, name: 'Regenerate', type: 'LocationsRegen', phase: 'Selection', description: 'Restore up to 2 used locations to your hand.' },
  4: { id: 4, name: 'Move Target', type: 'MoveTarget', phase: 'Result', description: 'Move creature 1 sector Left or Right.' },
  5: { id: 5, name: 'Fog', type: 'Fog', phase: 'Selection', description: 'Hide from the creature this round.' }
};

interface CardState {
  cards: SurvivalCard[];
  isLoading: boolean;
  fetchCards: (availableIds: number[]) => void;
  playCard: (gameId: string, cardId: number, targetLocationIds?: number[], direction?: string) => Promise<any>;
}

export const useCardStore = create<CardState>((set) => ({
  cards: [],
  isLoading: false,

  fetchCards: (availableIds: number[]) => {
    const availableCards = availableIds.map(id => CARDS_DICTIONARY[id]).filter(Boolean);
    set({ cards: availableCards, isLoading: false });
  },

  playCard: async (gameId: string, cardId: number, targetLocationIds: number[] = [], direction: string = "") => {
    set({ isLoading: true });
    try {
      let payload: any = undefined;
      const hasTargets = targetLocationIds && targetLocationIds.length > 0;
      const hasDirection = direction && direction.trim() !== "";

      if (hasTargets || hasDirection) {
        payload = {};
        if (hasTargets) payload.targetLocationIds = targetLocationIds;
        if (hasDirection) payload.direction = direction;
      }

      const config = payload ? { headers: { 'Content-Type': 'application/json' } } : undefined;

      const response = await apiClient.post(`/game/${gameId}/cards/play/${cardId}`, payload, config);
      set({ isLoading: false });
      return response.data;
    } catch (err) {
      set({ isLoading: false });
      throw err;
    }
  }
}));