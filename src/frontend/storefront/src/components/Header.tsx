import { Link } from 'react-router-dom';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { logout } from '../store/slices/authSlice';

export default function Header() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();

  return (
    <header className="bg-white shadow">
      <nav className="max-w-6xl mx-auto px-4 py-4 flex justify-between items-center">
        {/* Logo */}
        <Link to="/" className="text-2xl font-bold text-blue-600">
          E-Commerce
        </Link>

        {/* Main Navigation */}
        <div className="flex gap-6 items-center">
          <Link to="/products" className="text-gray-700 hover:text-blue-600 font-medium">
            Products
          </Link>

          {/* Auth Links */}
          <div className="flex gap-4">
            {isAuthenticated ? (
              <>
                <span className="text-gray-700">
                  Hello, {user?.firstName}
                </span>
                <button
                  onClick={() => dispatch(logout())}
                  className="text-gray-700 hover:text-blue-600 font-medium"
                >
                  Logout
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="text-gray-700 hover:text-blue-600 font-medium">
                  Login
                </Link>
                <Link to="/register" className="text-gray-700 hover:text-blue-600 font-medium">
                  Register
                </Link>
              </>
            )}
          </div>

          {/* Cart Icon */}
          <Link to="/cart" className="relative">
            <svg className="w-6 h-6 text-gray-700 hover:text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
            <span className="absolute -top-2 -right-2 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
              0
            </span>
          </Link>
        </div>
      </nav>
    </header>
  );
}
