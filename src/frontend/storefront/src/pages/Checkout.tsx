import { useState } from 'react';

export default function Checkout() {
  const [step, setStep] = useState(1);

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-4xl mx-auto px-4">
        <h1 className="text-3xl font-bold mb-8">Checkout</h1>

        {/* Progress Steps */}
        <div className="flex justify-between mb-12">
          {[1, 2, 3].map((s) => (
            <div key={s} className="flex flex-col items-center">
              <div className={`w-10 h-10 rounded-full flex items-center justify-center font-bold ${
                s <= step ? 'bg-blue-600 text-white' : 'bg-gray-300 text-gray-600'
              }`}>
                {s}
              </div>
              <span className="mt-2 text-sm">
                {s === 1 ? 'Shipping' : s === 2 ? 'Payment' : 'Review'}
              </span>
            </div>
          ))}
        </div>

        {/* Step Content */}
        <div className="bg-white rounded-lg shadow p-8">
          {step === 1 && (
            <div>
              <h2 className="text-2xl font-bold mb-6">Shipping Address</h2>
              <form className="space-y-4" onSubmit={() => setStep(2)}>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <input type="text" placeholder="First Name" className="border p-2 rounded" />
                  <input type="text" placeholder="Last Name" className="border p-2 rounded" />
                  <input type="email" placeholder="Email" className="border p-2 rounded md:col-span-2" />
                  <input type="text" placeholder="Street Address" className="border p-2 rounded md:col-span-2" />
                  <input type="text" placeholder="City" className="border p-2 rounded" />
                  <input type="text" placeholder="State/Province" className="border p-2 rounded" />
                  <input type="text" placeholder="Postal Code" className="border p-2 rounded" />
                  <input type="text" placeholder="Country" className="border p-2 rounded" />
                  <input type="tel" placeholder="Phone" className="border p-2 rounded md:col-span-2" />
                </div>
                <button type="submit" className="w-full bg-blue-600 text-white py-3 rounded font-bold hover:bg-blue-700">
                  Continue to Payment
                </button>
              </form>
            </div>
          )}

          {step === 2 && (
            <div>
              <h2 className="text-2xl font-bold mb-6">Payment Method</h2>
              <form className="space-y-4" onSubmit={() => setStep(3)}>
                <div className="space-y-2">
                  <label className="flex items-center">
                    <input type="radio" name="payment" defaultChecked className="mr-2" />
                    Credit Card
                  </label>
                  <label className="flex items-center">
                    <input type="radio" name="payment" className="mr-2" />
                    PayPal
                  </label>
                </div>
                <input type="text" placeholder="Card Number" className="border p-2 rounded w-full" />
                <div className="grid grid-cols-2 gap-4">
                  <input type="text" placeholder="MM/YY" className="border p-2 rounded" />
                  <input type="text" placeholder="CVC" className="border p-2 rounded" />
                </div>
                <button type="submit" className="w-full bg-blue-600 text-white py-3 rounded font-bold hover:bg-blue-700">
                  Review Order
                </button>
              </form>
            </div>
          )}

          {step === 3 && (
            <div>
              <h2 className="text-2xl font-bold mb-6">Order Review</h2>
              <p className="text-gray-600 mb-6">Please review your order before placing it</p>
              <button className="w-full bg-green-600 text-white py-3 rounded font-bold hover:bg-green-700">
                Place Order
              </button>
            </div>
          )}

          {/* Navigation */}
          <div className="flex justify-between mt-6">
            <button
              onClick={() => setStep(Math.max(1, step - 1))}
              disabled={step === 1}
              className="px-6 py-2 border rounded disabled:opacity-50"
            >
              Back
            </button>
            {step > 1 && (
              <button
                onClick={() => setStep(Math.min(3, step + 1))}
                disabled={step === 3}
                className="px-6 py-2 border rounded disabled:opacity-50"
              >
                Next
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
