import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import PrivateRoute from './components/PrivateRoute';
import Navigation from './components/Navigation';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import GamePage from './pages/GamePage';
import './App.css';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <div className="app">
          <Navigation />
          <main className="main-content">
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route
                path="/game"
                element={
                  <PrivateRoute>
                    <GamePage />
                  </PrivateRoute>
                }
              />
              <Route path="/" element={<Navigate to="/game" replace />} />
            </Routes>
          </main>
        </div>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
