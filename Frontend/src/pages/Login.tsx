import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  
  const navigate = useNavigate();
  const token = useAuthStore((state) => state.token);
  const login = useAuthStore((state) => state.login);
  const isLoading = useAuthStore((state) => state.isLoading);
  const error = useAuthStore((state) => state.error);

  // Если токен уже есть, сразу кидаем в игру
  useEffect(() => {
    if (token) {
      navigate('/', { replace: true });
    }
  }, [token, navigate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (email.trim() && password.trim()) {
      try {
        await login(email, password);
        navigate('/', { replace: true });
      } catch (err) {
        // Ошибка уже обработана в authStore и выведена в UI
      }
    }
  };

  return (
    <div 
      className="min-h-screen flex items-center justify-center bg-cover bg-center relative overflow-hidden"
      style={{ 
        backgroundImage: `linear-gradient(to bottom, rgba(2, 6, 23, 0.7), rgba(2, 6, 23, 0.9)), url('https://github.com/Deseres/NewNotAloneGame/blob/main/Images/LocationsImages/bg-mainpage.jpeg?raw=true')` 
      }}
    >
      <div className="absolute inset-0 bg-slate-950/20 backdrop-blur-[2px]"></div>

      <div className="relative z-10 w-full max-w-md px-6 flex flex-col items-center">
        <h1 
          className="text-5xl md:text-7xl mb-12 text-slate-100 tracking-widest whitespace-nowrap font-black"
          style={{ fontFamily: "'Orbitron', sans-serif" }}
        >
          NOT ALONE
        </h1>

        <div className="bg-slate-900/80 backdrop-blur-md p-8 rounded-2xl border border-slate-700/50 shadow-2xl w-full transition-all duration-500 hover:border-slate-500/50">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <label 
                htmlFor="email" 
                className="block text-sm font-medium text-slate-400 mb-2 uppercase tracking-widest cursor-pointer"
                style={{ fontFamily: "'Orbitron', sans-serif" }}
              >
                Email
              </label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Enter email..."
                className="w-full bg-slate-950/50 border border-slate-700 rounded-xl px-4 py-3 text-white focus:outline-none focus:ring-2 focus:ring-red-600/50 focus:border-red-600/50 transition-all placeholder:text-slate-600 cursor-text"
                required
              />
            </div>

            <div>
              <label 
                htmlFor="password" 
                className="block text-sm font-medium text-slate-400 mb-2 uppercase tracking-widest cursor-pointer"
                style={{ fontFamily: "'Orbitron', sans-serif" }}
              >
                Password
              </label>
              <input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter password..."
                className="w-full bg-slate-950/50 border border-slate-700 rounded-xl px-4 py-3 text-white focus:outline-none focus:ring-2 focus:ring-red-600/50 focus:border-red-600/50 transition-all placeholder:text-slate-600 cursor-text"
                required
              />
            </div>

            {error && (
              <div className="text-red-400 text-sm bg-red-900/20 border border-red-900/50 px-3 py-2 rounded-lg">
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={isLoading || !email.trim() || !password.trim()}
              className="w-full bg-red-700 hover:bg-red-600 text-white font-black py-4 rounded-xl shadow-lg shadow-red-900/20 transition-all duration-300 transform hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50 disabled:cursor-not-allowed uppercase tracking-widest cursor-pointer"
              style={{ fontFamily: "'Orbitron', sans-serif" }}
            >
              {isLoading ? 'Processing...' : 'LOGIN'}
            </button>
          </form>

          <div className="mt-6 text-center text-slate-400 text-sm" style={{ fontFamily: "'Rajdhani', sans-serif" }}>
            <span>
              Don't have an account?{' '}
              <Link
                to="/register"
                className="text-white font-bold hover:underline transition-all cursor-pointer"
              >
                Register
              </Link>
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}