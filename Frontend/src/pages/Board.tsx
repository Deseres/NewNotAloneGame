import { useEffect, useState } from 'react';
import { PlugZap, LogOut } from 'lucide-react';
import { useAuthStore } from '../store/authStore';
import { useGameStore } from '../store/gameStore';
import { useCardStore } from '../store/cardStore';
import { LOCATIONS } from '../constants/locations';

const LOCATION_IMAGES: Record<number, string> = {
  1: "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/1Lair.png?raw=true",
  2: "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/2Jungles.png?raw=true",
  3: "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/3River.png?raw=true",
  5: "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/5Rover.png?raw=true",
  6: "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/6Swamp.png?raw=true",
  7: "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/7Shelter.png?raw=true",
  8: "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/8Wreck.png?raw=true",
  9: "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/9Source.png?raw=true",
  10: "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/10Artefact.png?raw=true"
};

const BEACH_LIT = "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/4BeachBeaconLit.png?raw=true";
const BEACH_NOT_LIT = "https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/4BeachBeaconNOTLit.png?raw=true";

export default function Board() {
  const logout = useAuthStore((state) => state.logout);
  const { session, message, isLoading: isGameLoading, error, fetchSession, setSessionData, startGame, playLocation, nextRound, resist, giveUp } = useGameStore();
  const { cards, fetchCards, playCard, isLoading: isCardLoading } = useCardStore();

  const [activeCardId, setActiveCardId] = useState<number | null>(null);
  const [cardTargets, setCardTargets] = useState<number[]>([]);
  const [isResistMode, setIsResistMode] = useState(false);
  const [resistTargets, setResistTargets] = useState<number[]>([]);
  
  const [isLoaderVisible, setIsLoaderVisible] = useState(true);
  const [loaderOpacity, setLoaderOpacity] = useState(100);

  const isLoading = isGameLoading || isCardLoading;

  const triggerLoader = () => {
    setIsLoaderVisible(true);
    setLoaderOpacity(100);
    setTimeout(() => setLoaderOpacity(0), 800);
    setTimeout(() => setIsLoaderVisible(false), 1800);
  };

  useEffect(() => {
    triggerLoader();
  }, []);

  useEffect(() => {
    const savedSessionId = localStorage.getItem('sessionId');
    if (savedSessionId && savedSessionId !== 'undefined') {
      fetchSession(savedSessionId);
    }
  }, []);

  useEffect(() => {
    if (session?.survivalCards) {
      fetchCards(session.survivalCards);
    }
  }, [session?.survivalCards]);

  const handleStartGame = async () => {
    triggerLoader();
    await startGame();
  };

  const handleDisconnect = () => {
    localStorage.removeItem('sessionId');
    window.location.reload(); 
  };

  const handlePlaySimpleCard = async (cardId: number) => {
    if (!session?.id) return;
    try {
      const data = await playCard(session.id, cardId, [], "");
      setSessionData(data);
    } catch (err) { console.error(err); }
  };

  const handleDirectionCard = async (cardId: number, direction: string) => {
    if (!session?.id) return;
    try {
      const data = await playCard(session.id, cardId, [], direction);
      setSessionData(data);
    } catch (err) { console.error(err); }
  };

  const handleConfirmTargets = async () => {
    if (!session?.id || !activeCardId) return;
    try {
      const data = await playCard(session.id, activeCardId, cardTargets, "");
      setSessionData(data);
      setActiveCardId(null);
      setCardTargets([]);
    } catch (err) { console.error(err); }
  };

  const handleCancelTargets = () => {
    setActiveCardId(null);
    setCardTargets([]);
  };

  const handleConfirmResist = async () => {
    if (!session?.id) return;
    try {
      await resist(resistTargets);
      setIsResistMode(false);
      setResistTargets([]);
    } catch (err) { console.error(err); }
  };

  const handleCancelResist = () => {
    setIsResistMode(false);
    setResistTargets([]);
  };

  const hexStyle = { clipPath: "polygon(50% 0%, 100% 25%, 100% 75%, 50% 100%, 0% 75%, 0% 25%)" };
  const usedLocationsCount = session?.usedLocations?.length || 0;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 pt-28 pb-6 px-6 flex flex-col relative overflow-x-hidden">
      
      {isLoaderVisible && (
        <div 
          className="fixed inset-0 z-[100] bg-black flex flex-col items-center justify-center transition-opacity duration-1000 ease-in-out pointer-events-none"
          style={{ opacity: loaderOpacity / 100 }}
        >
          <h2 className="text-emerald-500 text-2xl tracking-[0.5em] animate-pulse" style={{ fontFamily: "'Orbitron', sans-serif" }}>
            ENTERING ARTEMIA...
          </h2>
        </div>
      )}

      <header className="fixed top-0 left-0 w-full z-50 bg-slate-950/80 backdrop-blur-md border-b border-slate-800/50 shadow-lg px-6 py-4 flex justify-between items-center h-20">
        <div className="flex-1 flex justify-start items-center">
          {session && (
            <div className="hidden md:flex flex-col items-start" style={{ fontFamily: "'Rajdhani', sans-serif" }}>
              <span className="text-slate-400 text-base tracking-widest uppercase font-bold">
                Round: <span className="text-white mx-1 text-lg">{session.roundNumber}</span>
              </span>
              <span className="text-slate-400 text-base tracking-widest uppercase font-bold">
                Phase: <span className={`mx-1 text-lg ${session.currentPhase === 'Result' ? 'text-yellow-500' : 'text-blue-400'}`}>{session.currentPhase}</span>
              </span>
            </div>
          )}
        </div>

        <h1 className="text-3xl md:text-5xl font-black tracking-[0.2em] text-slate-100 whitespace-nowrap text-center flex-1" style={{ fontFamily: "'Orbitron', sans-serif" }}>
          NOT ALONE
        </h1>

        <div className="flex-1 flex justify-end items-center">
          {!session ? (
            <button onClick={logout} className="group flex items-center gap-2 text-slate-400 hover:text-red-500 transition-all z-10 cursor-pointer bg-slate-900/50 border border-slate-800 hover:border-red-900/50 px-4 py-2 rounded-lg shadow-lg">
              <span className="uppercase tracking-widest font-bold text-sm hidden sm:block" style={{ fontFamily: "'Rajdhani', sans-serif" }}>Logout</span>
              <LogOut size={20} className="group-hover:translate-x-1 transition-transform" />
            </button>
          ) : (
            <button onClick={handleDisconnect} className="group flex items-center gap-2 text-slate-400 hover:text-yellow-500 transition-all z-10 cursor-pointer bg-slate-900/50 border border-slate-800 hover:border-yellow-900/50 px-4 py-2 rounded-lg shadow-lg">
              <span className="uppercase tracking-widest font-bold text-sm hidden sm:block" style={{ fontFamily: "'Rajdhani', sans-serif" }}>Disconnect</span>
              <PlugZap size={20} className="group-hover:rotate-12 transition-transform" />
            </button>
          )}
        </div>
      </header>

      {!session ? (
        <div className="flex-1 flex flex-col items-center justify-center gap-6">
          <h2 className="text-2xl text-slate-400 tracking-widest uppercase" style={{ fontFamily: "'Rajdhani', sans-serif" }}>No active expedition found.</h2>
          <button onClick={handleStartGame} className="bg-emerald-700/80 hover:bg-emerald-600 text-white font-bold py-4 px-10 rounded-xl text-xl transition-all shadow-[0_0_20px_rgba(4,120,87,0.2)] hover:shadow-[0_0_30px_rgba(4,120,87,0.4)] tracking-widest border border-emerald-500/30 cursor-pointer" style={{ fontFamily: "'Orbitron', sans-serif" }}>
            START EXPEDITION
          </button>
        </div>
      ) : (
        <div className="flex flex-col gap-8 max-w-6xl mx-auto w-full relative z-10">
          
          {error && (
            <div className="bg-red-950/80 border border-red-600/50 text-red-200 p-4 rounded-xl text-center text-lg font-bold shadow-[0_0_20px_rgba(220,38,38,0.3)] animate-pulse w-full tracking-wider" style={{ fontFamily: "'Rajdhani', sans-serif" }}>
              ⚠️ {error}
            </div>
          )}

          {message && (
            <div className="bg-slate-800/80 backdrop-blur-md border border-slate-600/50 text-slate-200 p-4 rounded-xl text-center text-lg font-medium shadow-2xl animate-in fade-in slide-in-from-top-4 duration-500 tracking-wide" style={{ fontFamily: "'Rajdhani', sans-serif" }}>
              {message}
            </div>
          )}

          {/* Визуальный трек и панель команд */}
          <div className="bg-slate-900/40 backdrop-blur-sm border border-slate-800 rounded-xl p-6 shadow-xl flex flex-col items-center w-full">
            
            <div className="flex w-full justify-between px-4 mb-4">
              <span className="text-emerald-500 font-bold tracking-widest uppercase text-lg" style={{ fontFamily: "'Orbitron', sans-serif" }}>PROGRESS</span>
              <span className="text-purple-500 font-normal tracking-widest text-2xl drop-shadow-[0_0_8px_rgba(168,85,247,0.5)]" style={{ fontFamily: "'Creepster', system-ui" }}>ASSIMILATION</span>
            </div>

            <div className="flex items-center justify-center gap-1 sm:gap-2 mb-8">
              {[1, 2, 3, 4, 5, 6].map(i => (
                <div key={`p-${i}`} style={hexStyle} className={`w-8 h-9 sm:w-12 sm:h-14 flex items-center justify-center transition-all duration-500 ${session.playerProgress >= i ? 'bg-emerald-500 shadow-[0_0_15px_rgba(16,185,129,0.8)]' : 'bg-slate-800'}`}>
                  <div style={hexStyle} className="w-[90%] h-[90%] bg-slate-950 flex items-center justify-center">
                    <span className={`font-bold text-sm sm:text-base ${session.playerProgress >= i ? 'text-emerald-400' : 'text-slate-700'}`}>{i}</span>
                  </div>
                </div>
              ))}

              <div style={hexStyle} className="w-12 h-14 sm:w-16 sm:h-20 flex items-center justify-center bg-yellow-600 mx-2 shadow-[0_0_20px_rgba(234,179,8,0.5)]">
                 <div style={hexStyle} className="w-[85%] h-[85%] bg-slate-900 flex items-center justify-center">
                    <span className="text-yellow-500 text-2xl" style={{ fontFamily: "'Orbitron', sans-serif" }}>★</span>
                 </div>
              </div>

              {/* Трек монстра: от центра к краю 4, 3, 2, 1 */}
              {[4, 3, 2, 1].map(i => (
                <div key={`c-${i}`} style={hexStyle} className={`w-8 h-9 sm:w-12 sm:h-14 flex items-center justify-center transition-all duration-500 ${session.creatureProgress >= i ? 'bg-purple-600 shadow-[0_0_15px_rgba(147,51,234,0.8)]' : 'bg-slate-800'}`}>
                  <div style={hexStyle} className="w-[90%] h-[90%] bg-slate-950 flex items-center justify-center">
                    <span className={`font-normal text-lg sm:text-xl ${session.creatureProgress >= i ? 'text-purple-400' : 'text-slate-700'}`} style={{ fontFamily: "'Creepster', system-ui" }}>{i}</span>
                  </div>
                </div>
              ))}
            </div>

            <div className="flex flex-col items-center justify-center border-t border-slate-800/50 pt-6 w-full">
              <div className="flex flex-col items-center mb-4">
                 <span className="text-5xl font-black text-blue-400 drop-shadow-[0_0_10px_rgba(96,165,250,0.3)]" style={{ fontFamily: "'Orbitron', sans-serif" }}>{session.playerWillpower}</span>
                 <span className="text-slate-500 text-xs uppercase tracking-[0.2em] font-bold mt-1" style={{ fontFamily: "'Rajdhani', sans-serif" }}>Willpower</span>
              </div>
              
              <div className="flex flex-wrap justify-center gap-4 w-full">
                {isResistMode ? (
                  <div className="flex gap-2">
                    <button onClick={handleConfirmResist} disabled={resistTargets.length === 0 || isLoading} className="bg-yellow-600/80 hover:bg-yellow-500 text-white font-bold py-3 px-6 rounded-lg transition-all disabled:opacity-50 tracking-widest uppercase border border-yellow-500/50 cursor-pointer">Confirm Resist</button>
                    <button onClick={handleCancelResist} disabled={isLoading} className="bg-slate-800 hover:bg-slate-700 text-white font-bold py-3 px-6 rounded-lg transition-all disabled:opacity-50 tracking-widest uppercase border border-slate-600 cursor-pointer">Cancel</button>
                  </div>
                ) : (
                  <button onClick={() => setIsResistMode(true)} disabled={isLoading || session.playerWillpower <= 0 || usedLocationsCount === 0 || session.currentPhase !== 'Selection'} className="bg-indigo-900/60 hover:bg-indigo-800 text-indigo-200 font-bold py-3 px-6 rounded-lg transition-all disabled:opacity-30 tracking-widest uppercase border border-indigo-700/50 cursor-pointer">
                    Resist (-1 WP)
                  </button>
                )}
                
                <button onClick={giveUp} disabled={isLoading || usedLocationsCount === 0 || session.currentPhase !== 'Selection'} className="bg-red-950/60 hover:bg-red-900/80 text-red-300 font-bold py-3 px-6 rounded-lg transition-all disabled:opacity-30 tracking-widest uppercase border border-red-800/50 cursor-pointer">
                  Give Up (Reset)
                </button>

                <button onClick={nextRound} disabled={isLoading || session.currentPhase !== 'Result'} className="bg-emerald-700/80 hover:bg-emerald-600 text-white font-bold py-3 px-8 rounded-lg transition-all disabled:opacity-30 tracking-widest uppercase shadow-[0_0_15px_rgba(4,120,87,0.3)] border border-emerald-500/50 cursor-pointer" style={{ fontFamily: "'Orbitron', sans-serif" }}>
                  Next Round
                </button>
              </div>
            </div>
          </div>

          {(session.isBeaconLit || session.isFogActive || session.isRiverVisionActive || session.isArtefactActive) && (
            <div className="flex flex-wrap gap-4 justify-center" style={{ fontFamily: "'Rajdhani', sans-serif" }}>
              {session.isBeaconLit && <span className="bg-yellow-950/40 text-yellow-500 border border-yellow-700/50 px-5 py-2 rounded-full text-sm font-bold tracking-wider shadow-[0_0_15px_rgba(202,138,4,0.15)] uppercase">💡 Beacon Lit</span>}
              {session.isFogActive && <span className="bg-slate-800/60 text-slate-300 border border-slate-600/50 px-5 py-2 rounded-full text-sm font-bold tracking-wider shadow-[0_0_15px_rgba(100,116,139,0.15)] uppercase">🌫️ Fog Active</span>}
              {session.isRiverVisionActive && <span className="bg-cyan-950/40 text-cyan-400 border border-cyan-700/50 px-5 py-2 rounded-full text-sm font-bold tracking-wider shadow-[0_0_15px_rgba(8,145,178,0.15)] uppercase">👁️ River Vision</span>}
              {session.isArtefactActive && <span className="bg-purple-950/40 text-purple-400 border border-purple-700/50 px-5 py-2 rounded-full text-sm font-bold tracking-wider shadow-[0_0_15px_rgba(147,51,234,0.15)] uppercase">🔮 Artefact Active</span>}
            </div>
          )}

          {/* Текст для выбора таргетов появляется только по делу */}
          {(activeCardId === 3 || isResistMode) && (
            <h3 className="text-xl font-bold tracking-widest uppercase mt-4 text-yellow-500 animate-pulse drop-shadow-[0_0_8px_rgba(234,179,8,0.5)] text-center" style={{ fontFamily: "'Orbitron', sans-serif" }}>
              Select up to 2 used locations
            </h3>
          )}
          
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map((locId) => {
              const used = session.usedLocations || [];
              const isAvailable = (session.availableLocations || []).includes(locId);
              const isUsed = used.includes(locId);
              const isPlayerLocation = session.currentPhase === 'Result' && session.lastPlayerChoice === locId;
              const isCreatureLocation = session.currentPhase === 'Result' && session.creatureChosenLocation === locId;
              const isCaught = isPlayerLocation && isCreatureLocation;
              const locData = LOCATIONS[locId];

              const isTargetSelectionMode = activeCardId === 3 || isResistMode;
              const isSelectedTarget = isResistMode ? resistTargets.includes(locId) : cardTargets.includes(locId);

              let bgImage = LOCATION_IMAGES[locId];
              if (locId === 4) bgImage = session.isBeaconLit ? BEACH_LIT : BEACH_NOT_LIT;

              let buttonClasses = "aspect-[2/3] rounded-xl flex flex-col items-center border border-slate-800 transition-all duration-300 relative overflow-hidden group ";
              
              if (session.currentPhase === 'Result') {
                buttonClasses += "cursor-not-allowed ";
                if (isPlayerLocation && !isCaught) buttonClasses += "opacity-100 border-blue-500 shadow-[0_0_20px_rgba(37,99,235,0.4)] ";
                else if (isCreatureLocation && !isCaught) buttonClasses += "opacity-100 border-purple-500 shadow-[0_0_20px_rgba(147,51,234,0.4)] ";
                else if (isCaught) buttonClasses += "opacity-100 border-red-600 shadow-[0_0_30px_rgba(220,38,38,0.6)] animate-pulse ";
                else buttonClasses += "opacity-40 grayscale ";
              } else if (isTargetSelectionMode) {
                if (isUsed) {
                  buttonClasses += isSelectedTarget ? "border-yellow-500 ring-2 ring-yellow-500/50 -translate-y-1 cursor-pointer " : "opacity-60 hover:opacity-100 hover:border-yellow-600 cursor-pointer ";
                } else {
                  buttonClasses += "opacity-10 grayscale cursor-not-allowed ";
                }
              } else {
                if (isUsed) buttonClasses += "opacity-30 grayscale cursor-not-allowed ";
                else if (isAvailable) buttonClasses += "hover:border-emerald-500/50 hover:shadow-[0_0_20px_rgba(16,185,129,0.2)] hover:-translate-y-1 cursor-pointer ";
                else buttonClasses += "opacity-20 cursor-not-allowed ";
              }

              return (
                <button
                  key={locId}
                  disabled={!isTargetSelectionMode && (!isAvailable || session.currentPhase !== 'Selection')}
                  onClick={() => {
                    if (isResistMode && isUsed) {
                      setResistTargets(prev => prev.includes(locId) ? prev.filter(id => id !== locId) : (prev.length < 2 ? [...prev, locId] : prev));
                    } else if (activeCardId === 3 && isUsed) {
                      setCardTargets(prev => prev.includes(locId) ? prev.filter(id => id !== locId) : (prev.length < 2 ? [...prev, locId] : prev));
                    } else {
                      playLocation(locId);
                    }
                  }}
                  className={buttonClasses}
                  style={{ backgroundImage: `linear-gradient(to bottom, rgba(0,0,0,0.1), rgba(0,0,0,0.4)), url('${bgImage}')`, backgroundSize: 'cover', backgroundPosition: 'center' }}
                >
                  <div className="absolute inset-0 shadow-[inset_0_0_50px_rgba(0,0,0,0.9)] rounded-xl pointer-events-none z-0"></div>

                  <span className="text-4xl font-black text-white/30 absolute top-2 right-3 z-10" style={{ fontFamily: "'Orbitron', sans-serif" }}>{locId}</span>
                  
                  {session.currentPhase === 'Result' && (isPlayerLocation || isCreatureLocation) && (
                    <div className="absolute inset-0 flex flex-col items-center justify-center gap-2 z-20 backdrop-blur-[1px] bg-black/40">
                      {isCaught ? (
                        <span className="bg-red-700 text-white text-3xl px-4 py-2 rounded-lg font-black shadow-[0_0_20px_rgba(220,38,38,0.8)] tracking-widest drop-shadow-xl" style={{ fontFamily: "'Creepster', system-ui" }}>CAUGHT</span>
                      ) : (
                        <>
                          {isPlayerLocation && <span className="bg-blue-600 text-white text-xs px-3 py-1.5 rounded font-bold tracking-widest">SURVIVOR</span>}
                          {isCreatureLocation && <span className="bg-purple-900/90 text-purple-300 border border-purple-500 text-xl px-3 py-1 rounded font-bold drop-shadow-[0_0_10px_rgba(168,85,247,0.8)]" style={{ fontFamily: "'Creepster', system-ui" }}>CREATURE</span>}
                        </>
                      )}
                    </div>
                  )}

                  <div className="mt-auto w-full p-3 text-center bg-gradient-to-t from-black via-black/80 to-transparent z-10">
                    <span className="text-base font-bold text-white tracking-wider block mb-1" style={{ fontFamily: "'Orbitron', sans-serif" }}>
                      {locData?.name || `Sector ${locId}`}
                    </span>
                    <span className="text-[10px] leading-relaxed text-slate-300 opacity-90 uppercase tracking-wider block" style={{ fontFamily: "'Rajdhani', sans-serif" }}>
                      {locData?.effect}
                    </span>
                  </div>
                </button>
              );
            })}
          </div>

          <div className="mt-4 border-t border-slate-800/50 pt-8 pb-12">
            <h3 className="text-xl font-bold text-slate-400 mb-6 tracking-widest uppercase" style={{ fontFamily: "'Orbitron', sans-serif" }}>Survival Tools</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
              {cards.map((card) => {
                const isAvailableToPlay = session.availableSurvivalCards?.includes(card.id) || false;
                const isHealFull = card.id === 1 && session.playerWillpower >= 3;

                return (
                  <div key={card.id} className={`bg-slate-900/40 backdrop-blur-sm border p-6 rounded-xl flex flex-col transition-all border-slate-800 ${isAvailableToPlay ? 'hover:border-slate-600' : 'opacity-50 grayscale'}`}>
                    <div className="flex justify-between items-start mb-4">
                      <span className="font-bold text-lg text-slate-100 tracking-wider" style={{ fontFamily: "'Orbitron', sans-serif" }}>{card.name}</span>
                      <span className="text-[10px] font-bold tracking-widest uppercase bg-slate-950 px-2 py-1 rounded text-slate-500 border border-slate-800">{card.phase}</span>
                    </div>
                    <p className="text-sm text-slate-400 mb-6 flex-1 leading-relaxed" style={{ fontFamily: "'Rajdhani', sans-serif" }}>{card.description}</p>
                    
                    <div className="mt-auto relative z-10" style={{ fontFamily: "'Rajdhani', sans-serif" }}>
                      {card.id === 3 ? (
                        activeCardId === 3 ? (
                          <div className="flex gap-2">
                            <button
                              onClick={handleConfirmTargets}
                              disabled={cardTargets.length === 0 || isLoading}
                              className="flex-1 bg-yellow-600/80 hover:bg-yellow-500 text-white py-2.5 px-4 rounded-lg font-bold transition-all disabled:opacity-50 disabled:cursor-not-allowed uppercase tracking-wider border border-yellow-500/50 cursor-pointer"
                            >
                              Confirm
                            </button>
                            <button
                              onClick={handleCancelTargets}
                              disabled={isLoading}
                              className="flex-1 bg-slate-800 hover:bg-slate-700 text-white py-2.5 px-4 rounded-lg font-bold transition-all disabled:opacity-50 uppercase tracking-wider border border-slate-600 cursor-pointer"
                            >
                              Cancel
                            </button>
                          </div>
                        ) : (
                          <button
                            onClick={() => setActiveCardId(3)}
                            disabled={!isAvailableToPlay || isLoading || usedLocationsCount === 0}
                            className={`w-full py-2.5 px-4 rounded-lg font-bold transition-all uppercase tracking-widest border ${isAvailableToPlay && usedLocationsCount > 0 ? 'bg-emerald-900/40 text-emerald-400 border-emerald-700/50 hover:bg-emerald-800/60 hover:text-white cursor-pointer' : 'bg-black/40 text-slate-600 border-slate-800 cursor-not-allowed'}`}
                          >
                            {usedLocationsCount === 0 ? 'No Used Locations' : 'Select Targets'}
                          </button>
                        )
                      ) : card.id === 4 ? (
                        <div className="flex gap-2">
                          <button
                            onClick={() => handleDirectionCard(4, 'Left')}
                            disabled={!isAvailableToPlay || isLoading || session.creatureChosenLocation === 1}
                            className={`flex-1 py-2.5 px-4 rounded-lg font-bold transition-all uppercase tracking-wider border ${isAvailableToPlay && session.creatureChosenLocation !== 1 ? 'bg-blue-900/40 text-blue-400 border-blue-700/50 hover:bg-blue-800/60 hover:text-white cursor-pointer' : 'bg-black/40 text-slate-600 border-slate-800 cursor-not-allowed'}`}
                          >
                            Left
                          </button>
                          <button
                            onClick={() => handleDirectionCard(4, 'Right')}
                            disabled={!isAvailableToPlay || isLoading || session.creatureChosenLocation === 10}
                            className={`flex-1 py-2.5 px-4 rounded-lg font-bold transition-all uppercase tracking-wider border ${isAvailableToPlay && session.creatureChosenLocation !== 10 ? 'bg-blue-900/40 text-blue-400 border-blue-700/50 hover:bg-blue-800/60 hover:text-white cursor-pointer' : 'bg-black/40 text-slate-600 border-slate-800 cursor-not-allowed'}`}
                          >
                            Right
                          </button>
                        </div>
                      ) : (
                        <button
                          onClick={() => handlePlaySimpleCard(card.id)}
                          disabled={!isAvailableToPlay || isLoading || isHealFull}
                          className={`w-full py-2.5 px-4 rounded-lg font-bold transition-all uppercase tracking-widest border ${isAvailableToPlay && !isHealFull ? 'bg-emerald-900/40 text-emerald-400 border-emerald-700/50 hover:bg-emerald-800/60 hover:text-white cursor-pointer' : 'bg-black/40 text-slate-600 border-slate-800 cursor-not-allowed'}`}
                        >
                          {isHealFull ? 'Willpower Full' : 'Deploy'}
                        </button>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      )}

      {session?.isGameOver && (
        <div className="fixed inset-0 bg-black/90 backdrop-blur-md z-[100] flex flex-col items-center justify-center p-4">
          <h2 
            className={`text-7xl md:text-9xl mb-8 tracking-tighter drop-shadow-[0_0_30px_currentColor] animate-pulse ${session.playerProgress > session.creatureProgress ? 'text-emerald-500' : 'text-red-600'}`}
            style={{ fontFamily: session.playerProgress > session.creatureProgress ? "'Orbitron', sans-serif" : "'Creepster', system-ui" }}
          >
            {session.playerProgress > session.creatureProgress ? 'VICTORY' : 'DEFEAT'}
          </h2>
          <button onClick={handleStartGame} disabled={isLoading} className="bg-red-900 hover:bg-red-800 text-white font-black py-4 px-12 rounded-xl text-xl transition-all shadow-[0_0_20px_rgba(153,27,27,0.4)] disabled:opacity-50 tracking-[0.3em] uppercase border border-red-700/50 cursor-pointer" style={{ fontFamily: "'Orbitron', sans-serif" }}>
            Restart Expedition
          </button>
        </div>
      )}
    </div>
  );
}