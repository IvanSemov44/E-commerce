import { Link } from 'react-router-dom';

export default function Footer() {
  return (
    <footer className="bg-gray-800 text-gray-300 mt-16">
      <div className="max-w-6xl mx-auto px-4 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8 mb-8">
          {/* Company */}
          <div>
            <h3 className="text-white font-bold text-lg mb-4">Company</h3>
            <ul className="space-y-2">
              <li><Link to="/" className="hover:text-white">About Us</Link></li>
              <li><Link to="/" className="hover:text-white">Careers</Link></li>
              <li><Link to="/" className="hover:text-white">Press</Link></li>
              <li><Link to="/" className="hover:text-white">Blog</Link></li>
            </ul>
          </div>

          {/* Support */}
          <div>
            <h3 className="text-white font-bold text-lg mb-4">Support</h3>
            <ul className="space-y-2">
              <li><Link to="/" className="hover:text-white">Help Center</Link></li>
              <li><Link to="/" className="hover:text-white">Contact Us</Link></li>
              <li><Link to="/" className="hover:text-white">Track Order</Link></li>
              <li><Link to="/" className="hover:text-white">Returns</Link></li>
            </ul>
          </div>

          {/* Legal */}
          <div>
            <h3 className="text-white font-bold text-lg mb-4">Legal</h3>
            <ul className="space-y-2">
              <li><Link to="/" className="hover:text-white">Privacy Policy</Link></li>
              <li><Link to="/" className="hover:text-white">Terms of Service</Link></li>
              <li><Link to="/" className="hover:text-white">Cookies</Link></li>
              <li><Link to="/" className="hover:text-white">Security</Link></li>
            </ul>
          </div>

          {/* Newsletter */}
          <div>
            <h3 className="text-white font-bold text-lg mb-4">Newsletter</h3>
            <p className="text-sm mb-4">Subscribe to get special offers and updates</p>
            <form className="flex" onSubmit={(e) => e.preventDefault()}>
              <input
                type="email"
                placeholder="Your email"
                className="flex-grow px-3 py-2 rounded-l text-black"
              />
              <button className="bg-blue-600 px-4 py-2 rounded-r hover:bg-blue-700">
                Subscribe
              </button>
            </form>
          </div>
        </div>

        {/* Bottom */}
        <div className="border-t border-gray-700 pt-8 flex justify-between items-center">
          <p>&copy; 2026 E-Commerce. All rights reserved.</p>
          <div className="flex gap-4">
            <a href="#" className="hover:text-white">Facebook</a>
            <a href="#" className="hover:text-white">Twitter</a>
            <a href="#" className="hover:text-white">Instagram</a>
          </div>
        </div>
      </div>
    </footer>
  );
}
