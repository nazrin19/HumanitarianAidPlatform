"use client";

import Image from "next/image";
import Link from "next/link";
import { FormEvent, useRef, useState } from "react";
import styles from "./page.module.css";

type LoginField = "role" | "username" | "password";

export default function LoginPage() {
  const [role, setRole] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [isSuccess, setIsSuccess] = useState(false);
  const [invalidField, setInvalidField] = useState<LoginField | null>(null);
  const roleRef = useRef<HTMLSelectElement>(null);
  const usernameRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setIsSuccess(false);
    setInvalidField(null);

    if (!role) {
      setInvalidField("role");
      setError("Select your role.");
      roleRef.current?.focus();
      return;
    }

    if (!username.trim()) {
      setInvalidField("username");
      setError("Enter your username.");
      usernameRef.current?.focus();
      return;
    }

    if (!password) {
      setInvalidField("password");
      setError("Enter your password.");
      passwordRef.current?.focus();
      return;
    }

    setIsSuccess(true);
    setError("Signed in. Redirecting to your dashboard…");
  }

  return (
    <main
      className={`${styles.page} min-h-screen flex items-center justify-center p-4 md:p-6`}
    >
      <div
        className={`${styles.shell} w-full max-w-270 min-h-172.5 rounded-[28px] overflow-hidden grid grid-cols-2 bg-white shadow-xl`}
      >
        <section className={styles.formPanel}>
          <div className={styles.brand}>
            <Image
              className={styles.logo}
              src="/humanitarian-aid-platform-logo.svg"
              alt="Humanitarian Aid Platform logo"
              width={50}
              height={50}
            />
            <span>Humanitarian Aid Platform</span>
          </div>

          <div className={styles.main}>
            <h1>Welcome back!</h1>
            <p className={styles.subtitle}>
              Manage requests, coordinate resources, and track aid dispatch.
            </p>

            <form noValidate onSubmit={handleSubmit}>
              <div className={styles.field}>
                <label htmlFor="login-role">Role</label>
                <select
                  ref={roleRef}
                  id="login-role"
                  aria-label="Role"
                  aria-invalid={invalidField === "role"}
                  className={`${styles.roleSelect} ${role ? "" : styles.rolePlaceholder}`}
                  value={role}
                  onChange={(event) => setRole(event.target.value)}
                >
                  <option value="" disabled>
                    Select your role
                  </option>
                  <option value="Coordinator">Coordinator</option>
                  <option value="Shelter manager">Shelter manager</option>
                  <option value="Volunteer">Volunteer</option>
                  <option value="Donor">Donor</option>
                </select>
              </div>

              <div className={styles.field}>
                <label htmlFor="login-username">Username</label>
                <input
                  ref={usernameRef}
                  id="login-username"
                  type="text"
                  autoComplete="username"
                  aria-invalid={invalidField === "username"}
                  placeholder="Enter your username"
                  value={username}
                  onChange={(event) => setUsername(event.target.value)}
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

              <a className={styles.forgot} href="#forgot">
                Forgot password?
              </a>
              <div
                className={`${styles.error} ${isSuccess ? styles.success : ""}`}
                role="alert"
                aria-live="polite"
              >
                {error}
              </div>
              <button className={styles.button} type="submit">
                Login
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
