export interface LocationData {
  id: number;
  name: string;
  effect: string;
  safety: number;
  strategy: string;
}

export const LOCATIONS: Record<number, LocationData> = {
  1: {
    id: 1,
    name: "Lair",
    effect: "If escape: copies creature's effect. If caught: lose extra willpower (2 total).",
    safety: 0,
    strategy: "High risk, high reward - study creature strategy"
  },
  2: {
    id: 2,
    name: "Jungle",
    effect: "If escape: restore one random used location.",
    safety: 40,
    strategy: "Mid-game recovery for locations"
  },
  3: {
    id: 3,
    name: "River",
    effect: "If escape: enables River Vision (see creature's next move).",
    safety: 45,
    strategy: "Powerful intel for next round prediction"
  },
  4: {
    id: 4,
    name: "Beach",
    effect: "If escape with beacon lit: gain +1 progress. Can extinguish creature's beacon.",
    safety: 35,
    strategy: "Beacon manipulation - key tactical location"
  },
  5: {
    id: 5,
    name: "Rover",
    effect: "If escape: unlock one randomly blocked location.",
    safety: 50,
    strategy: "Expand options when trapped"
  },
  6: {
    id: 6,
    name: "Swamp",
    effect: "If escape: preserved in hand (can reuse next round).",
    safety: 48,
    strategy: "Strategic recycling for survival cards"
  },
  7: {
    id: 7,
    name: "Shelter",
    effect: "If escape: preserved in hand (can reuse next round).",
    safety: 52,
    strategy: "Safe card regeneration"
  },
  8: {
    id: 8,
    name: "Wreck",
    effect: "If escape: gain +1 progress toward victory.",
    safety: 38,
    strategy: "High-value escape location"
  },
  9: {
    id: 9,
    name: "Source",
    effect: "If escape: restore +1 willpower (max 3).",
    safety: 55,
    strategy: "Healing location - creature hunts here"
  },
  10: {
    id: 10,
    name: "Artefact",
    effect: "If escape: disable creature's next modifier.",
    safety: 60,
    strategy: "Neutralizes creature power - safest location"
  }
};