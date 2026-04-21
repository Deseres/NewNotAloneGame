import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import '../styles/Navigation.css';

const Navigation = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="navigation">
      <div className="nav-brand">
        <h2>Not Alone</h2>
      </div>
      <ul className="nav-links">
        <li><a href="/game">Игра</a></li>
        <li><a href="/survival">Выживание</a></li>
        <li><a href="/trade">Торговля</a></li>
      </ul>
      <div className="nav-user">
        <span>{user?.username}</span>
        <button onClick={handleLogout}>Выход</button>
      </div>
    </nav>
  );
};

export default Navigation;
