"use client";

import Image from "next/image";
import Link from "next/link";
import { FormEvent, useRef, useState } from "react";
import { setSessionCredentials } from "../lib/auth-session";
import styles from "./page.module.css";

type LoginField = "email" | "password";

type LoginResponse = {
  token?: string;
  role?: string;
};

const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001"
).replace(/\/+$/, "");

const ROLE_DESTINATIONS: Record<string, string> = {
  Citizen: "/dashboard",
  Coordinator: "/dashboard",
  CaseManager: "/dashboard",
  ResourceManager: "/dashboard",
  ShelterAdmin: "/dashboard",
  Volunteer: "/portal",
  Donor: "/portal",
  VolunteerCoordinator: "/portal",
  DonorOfficer: "/portal",
  PartnerOfficer: "/portal",
  Admin: "/admin",
};

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [invalidField, setInvalidField] = useState<LoginField | null>(null);
  const emailRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;
    setError("");
    setInvalidField(null);

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setInvalidField("email");
      setError("Enter a valid email address.");
      emailRef.current?.focus();
      return;
    }

    if (!password) {
      setInvalidField("password");
      setError("Enter your password.");
      passwordRef.current?.focus();
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: email.trim(), password }),
      });

      const responseText = await response.text();
      let responseBody: unknown;
      if (responseText) {
        try {
          responseBody = JSON.parse(responseText);
        } catch {
          if (response.ok) {
            throw new Error("The server returned an invalid login response.");
          }
        }
      }

      if (!response.ok) {
        let responseMessage: string | undefined;
        if (responseBody && typeof responseBody === "object") {
          const body = responseBody as {
            detail?: unknown;
            message?: unknown;
            title?: unknown;
          };
          const message = body.detail ?? body.message ?? body.title;
          if (typeof message === "string") responseMessage = message;
        }
        throw new Error(
          responseMessage ||
            `Login failed (${response.status} ${response.statusText}).`,
        );
      }

      if (!responseBody || typeof responseBody !== "object") {
        throw new Error("The server returned an invalid login response.");
      }

      const result = responseBody as LoginResponse;
      if (!result.token || !result.role) {
        throw new Error("The server response is missing the token or role.");
      }

      const destination = ROLE_DESTINATIONS[result.role];
      if (!destination) {
        throw new Error("Your account role is not recognized.");
      }

      setSessionCredentials(result.token, result.role);
      window.location.assign(destination);
    } catch (submitError) {
      setError(
        submitError instanceof Error
          ? submitError.message
          : "Unable to log in. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main
      className={`${styles.page} min-h-screen flex items-center justify-center p-4 md:p-6`}
    >
      <div
        className={`${styles.shell} w-full max-w-270 min-h-172.5 rounded-[28px] overflow-hidden grid grid-cols-2 bg-white shadow-xl`}
      >
        <section className={styles.formPanel}>
          <Link className={styles.brand} href="/" aria-label="Go to Humanitarian Aid Platform home">
            <Image
              className={styles.logo}
              src="/humanitarian-aid-platform-logo.svg"
              alt="Humanitarian Aid Platform logo"
              width={50}
              height={50}
            />
            <span>Humanitarian Aid Platform</span>
          </Link>

          <div className={styles.main}>
            <h1>Welcome back!</h1>
            <p className={styles.subtitle}>
              Manage requests, coordinate resources, and track aid dispatch.
            </p>

            <form noValidate onSubmit={handleSubmit}>
              <div className={styles.field}>
                <label htmlFor="login-email">Email Address</label>
                <input
                  ref={emailRef}
                  id="login-email"
                  type="email"
                  autoComplete="email"
                  aria-invalid={invalidField === "email"}
                  placeholder="Enter your email address"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                />
              </div>

              <div className={`${styles.field} ${styles.passwordField}`}>
                <label htmlFor="login-password">Password</label>
                <input
                  ref={passwordRef}
                  id="login-password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  aria-invalid={invalidField === "password"}
                  placeholder="••••••••"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                />
                <button
                  className={styles.eye}
                  type="button"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                  onClick={() => setShowPassword((visible) => !visible)}
                >
                  <svg
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    aria-hidden="true"
                  >
                    <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z" />
                    <circle cx="12" cy="12" r="3" />
                    <path
                      d="M4 4l16 16"
                      style={{ display: showPassword ? "block" : "none" }}
                    />
                  </svg>
                </button>
              </div>

              <span className={styles.forgot} title="Password recovery is not configured yet">
                Forgot password?
              </span>
              <div
                className={styles.error}
                role="alert"
                aria-live="polite"
              >
                {error}
              </div>
              <button
                className={styles.button}
                type="submit"
                disabled={isSubmitting}
              >
                {isSubmitting ? "Signing in..." : "Login"}
              </button>
            </form>

            <p className={styles.register}>
              Not a member? <Link href="/signup">Register here</Link>
            </p>
          </div>
        </section>

        <aside
          className={styles.illustrationPanel}
          aria-label="Platform overview"
        >
          <Image
            className={styles.overviewImage}
            src="/login-overview.jpg"
            alt="Aid workers unloading a supply truck, packing food and medical boxes near a shelter tent, with an Active Community 92 percent indicator. Empowering Crisis Response and Aid Coordination"
            width={1062}
            height={1264}
            sizes="(max-width: 900px) 0px, 50vw"
          />
        </aside>
      </div>
    </main>
  );
}
