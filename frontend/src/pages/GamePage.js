import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import * as gameService from '../services/gameService';
import '../styles/Game.css';

const GamePage = () => {
  const { user } = useAuth();
  const [sessions, setSessions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    loadGameSessions();
  }, []);

  const loadGameSessions = async () => {
    try {
      setError('');
      const data = await gameService.getGameSessions();
      setSessions(data);
    } catch (err) {
      setError('Ошибка при загрузке игровых сессий');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateSession = async () => {
    try {
      setError('');
      const newSession = await gameService.createGameSession({
        playerCount: 2,
      });
      setSessions([...sessions, newSession]);
    } catch (err) {
      setError('Ошибка при создании сессии');
    }
  };

  if (loading) return <div>Загрузка...</div>;

  return (
    <div className="game-container">
      <h1>Not Alone - Игровые сессии</h1>
      <p>Добро пожаловать, {user?.username}!</p>
      
      {error && <div className="error-message">{error}</div>}
      
      <button onClick={handleCreateSession}>Создать новую сессию</button>
      
      <div className="sessions-list">
        {sessions.length === 0 ? (
          <p>Нет активных сессий</p>
        ) : (
          sessions.map((session) => (
            <div key={session.id} className="session-card">
              <h3>Сессия {session.id}</h3>
              <p>Раунд: {session.roundNumber}</p>
              <p>Статус: {session.status}</p>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default GamePage;
