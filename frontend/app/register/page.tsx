"use client";

import { useState } from "react";
import { motion } from "framer-motion";
import { Mail, Lock, ArrowRight, AlertCircle, CheckCircle } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { EmailVerificationModal } from "@/components/auth/EmailVerificationModal";
import { useToast } from "@/hooks/useToast";

export default function RegisterPage() {
  const router = useRouter();
  const { register } = useAuth();
  const toast = useToast();
  const [formData, setFormData] = useState({
    email: "",
    password: "",
    confirmPassword: "",
  });
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [showVerificationModal, setShowVerificationModal] = useState(false);

  const passwordStrength = (password: string) => {
    if (password.length === 0) return { strength: 0, label: "" };
    if (password.length < 6) return { strength: 1, label: "Weak", color: "bg-red-500" };
    if (password.length < 10) return { strength: 2, label: "Medium", color: "bg-yellow-500" };
    if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(password)) 
      return { strength: 2, label: "Medium", color: "bg-yellow-500" };
    return { strength: 3, label: "Strong", color: "bg-green-500" };
  };

  const strength = passwordStrength(formData.password);

  const handleVerificationSuccess = () => {
    // User has verified email, redirect to dashboard
    router.push('/dashboard');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (formData.password !== formData.confirmPassword) {
      setError("Passwords do not match");
      return;
    }

    if (formData.password.length < 8) {
      setError("Password must be at least 8 characters");
      return;
    }

    setIsLoading(true);

    try {
      // Generate username from email (part before @)
      const username = formData.email.split('@')[0];
      await register(formData.email, username, formData.password);
      
      // Show verification modal instead of redirecting
      setShowVerificationModal(true);
      
      toast.info(
        'Registration Successful',
        'Please verify your email to continue'
      );
    } catch (err: any) {
      setError(err.message || "Registration failed");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-black text-white flex items-center justify-center p-6">
      <div className="absolute inset-0 bg-gradient-to-br from-blue-600/20 via-purple-600/20 to-pink-600/20 opacity-50" />
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,_var(--tw-gradient-stops))] from-blue-900/20 via-transparent to-transparent" />

      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="relative z-10 w-full max-w-md"
      >
        <div className="bg-white/10 backdrop-blur-xl border border-white/20 rounded-3xl p-8">
          <div className="text-center mb-8">
            <Link href="/" className="inline-block mb-6">
              <div className="text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                ServerEye
              </div>
            </Link>
            <h1 className="text-3xl font-bold mb-2">Create Account</h1>
            <p className="text-gray-400">Start monitoring your servers today</p>
          </div>

          {error && (
            <motion.div
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              className="mb-6 p-4 bg-red-500/10 border border-red-500/20 rounded-xl flex items-center gap-3"
            >
              <AlertCircle className="w-5 h-5 text-red-400 flex-shrink-0" />
              <p className="text-sm text-red-400">{error}</p>
            </motion.div>
          )}

          <form onSubmit={handleSubmit} className="space-y-6">
            <Input
              type="email"
              label="Email"
              placeholder="you@example.com"
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              required
              disabled={isLoading}
            />

            <div>
              <Input
                type="password"
                label="Password"
                placeholder="••••••••"
                value={formData.password}
                onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                required
                disabled={isLoading}
              />
              {formData.password && (
                <div className="mt-3">
                  <div className="flex gap-2 mb-2">
                    {[1, 2, 3].map((level) => (
                      <div
                        key={level}
                        className={`h-1 flex-1 rounded-full transition-colors ${
                          level <= strength.strength ? strength.color : 'bg-gray-700'
                        }`}
                      />
                    ))}
                  </div>
                  <p className="text-xs text-gray-400">
                    Password strength: <span className={strength.strength >= 2 ? 'text-green-400' : 'text-yellow-400'}>{strength.label}</span>
                  </p>
                </div>
              )}
            </div>

            <Input
              type="password"
              label="Confirm Password"
              placeholder="••••••••"
              value={formData.confirmPassword}
              onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
              required
              disabled={isLoading}
            />

            <div className="space-y-3 text-sm">
              <label className="flex items-start gap-3 cursor-pointer">
                <input type="checkbox" required className="mt-1 rounded" />
                <span className="text-gray-400">
                  I agree to the{" "}
                  <Link href="/terms" className="text-blue-400 hover:text-blue-300">
                    Terms of Service
                  </Link>{" "}
                  and{" "}
                  <Link href="/privacy" className="text-blue-400 hover:text-blue-300">
                    Privacy Policy
                  </Link>
                </span>
              </label>
            </div>

            <Button type="submit" fullWidth isLoading={isLoading}>
              <span className="flex items-center gap-2">
                Create Account
                <ArrowRight className="w-5 h-5" />
              </span>
            </Button>
          </form>

          <div className="mt-8 text-center text-sm">
            <span className="text-gray-400">Already have an account? </span>
            <Link href="/login" className="text-blue-400 hover:text-blue-300 font-semibold">
              Sign in
            </Link>
          </div>
        </div>
      </motion.div>

      {/* Email Verification Modal */}
      <EmailVerificationModal
        isOpen={showVerificationModal}
        onClose={() => setShowVerificationModal(false)}
        email={formData.email}
        onSuccess={handleVerificationSuccess}
      />
    </main>
  );
}
