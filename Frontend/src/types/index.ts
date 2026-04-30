export interface User {
  id: string;
  email: string;
}

export interface GameSession {
  id: string;
  createdAt: string;
  playerWillpower: number;
  playerProgress: number;
  creatureProgress: number;
  lastCreatureChoice: number;
  lastPlayerChoice: number;
  isGameOver: boolean;
  isBeaconLit: boolean;
  isRiverVisionActive: boolean;
  isRiverVisionRevealed: boolean;
  isFogActive: boolean;
  statusMessage: string;
  currentPhase: 'Selection' | 'Result';
  availableSurvivalCards: number[];
  survivalCards: number[];
  usedSurvivalCards: number[];
  activeCardEffects: number[];
  locations: number[];
  roundNumber: number;
  availableLocations: number[];
  usedLocations: number[];
  creatureChosenLocation: number;
  isArtefactActive: boolean;
  currentModifier: string;
}

export type CardType = 'Heal' | 'Beacon' | 'LocationsRegen' | 'MoveTarget' | 'Fog';

export interface SurvivalCard {
  id: number;
  name: string;
  type: CardType;
  phase: 'Selection' | 'Result';
  description: string;
}