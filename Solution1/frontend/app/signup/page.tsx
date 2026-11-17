"use client";

import Image from "next/image";
import Link from "next/link";
import { FormEvent, useRef, useState } from "react";
import styles from "./page.module.css";

type SignupField = "name" | "email" | "password" | "confirmPassword" | "terms";

export default function SignupPage() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState("");
  const [isSuccess, setIsSuccess] = useState(false);
  const [invalidField, setInvalidField] = useState<SignupField | null>(null);
  const nameRef = useRef<HTMLInputElement>(null);
  const emailRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);
  const confirmPasswordRef = useRef<HTMLInputElement>(null);
  const termsRef = useRef<HTMLInputElement>(null);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setIsSuccess(false);
    setInvalidField(null);

    if (!name.trim()) {
      setInvalidField("name");
      setError("Enter your full name.");
      nameRef.current?.focus();
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setInvalidField("email");
      setError("Enter a valid email address.");
      emailRef.current?.focus();
      return;
    }

    if (password.length < 8) {
      setInvalidField("password");
      setError("Use at least 8 characters for your password.");
      passwordRef.current?.focus();
      return;
    }

    if (password !== confirmPassword) {
      setInvalidField("confirmPassword");
      setError("Passwords do not match.");
      confirmPasswordRef.current?.focus();
      return;
    }

    if (!acceptedTerms) {
      setInvalidField("terms");
      setError("Agree to the Terms of Service and Privacy Policy to continue.");
      termsRef.current?.focus();
      return;
    }

    setIsSuccess(true);
    setError("Account created. Welcome to the community!");
  }

  return (
    <main
      className={`${styles.page} min-h-screen flex items-center justify-center p-4 md:p-6`}
    >
      <div
        className={`${styles.shell} w-full max-w-[1080px]`}
      >
        <section className={styles.left}>
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

          <div className={styles.art}>
            <Image
              src="/signup-community.jpg"
              alt="Volunteers carrying a supply box, planting a young tree together, and reaching out to help"
              width={940}
              height={640}
              className={styles.artImage}
              sizes="(max-width: 900px) 0px, 45vw"
            />
          </div>
          <h2>Together, we can make a difference</h2>
          <p>
            Join our community to help distribute aid, manage shelters, and
            support those in need.
          </p>
          <div className={styles.dots} aria-hidden="true">
            <i />
            <i />
            <i />
          </div>
        </section>

        <section className={styles.right}>
          <p className={styles.have}>
            Already have an account? <Link href="/login">Login</Link>
          </p>

          <div className={styles.form}>
            <h1>Create Account</h1>
            <p className={styles.sub}>
              Get started by filling out your details below.
            </p>

            <form noValidate onSubmit={handleSubmit}>
              <div className={styles.field}>
                <label htmlFor="signup-name">Full Name</label>
                <input
                  ref={nameRef}
                  id="signup-name"
                  type="text"
                  autoComplete="name"
                  aria-invalid={invalidField === "name"}
                  placeholder="Type your name"
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                />
              </div>

              <div className={styles.field}>
                <label htmlFor="signup-email">Email Address</label>
                <input
                  ref={emailRef}
                  id="signup-email"
                  type="email"
                  autoComplete="email"
                  aria-invalid={invalidField === "email"}
                  placeholder="Type email address"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                />
              </div>

              <div className={styles.row}>
                <div className={`${styles.field} ${styles.passwordField}`}>
                  <label htmlFor="signup-password">Password</label>
                  <input
                    ref={passwordRef}
                    id="signup-password"
                    type={showPassword ? "text" : "password"}
                    autoComplete="new-password"
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

                <div className={`${styles.field} ${styles.passwordField}`}>
                  <label htmlFor="signup-confirm-password">
                    Confirm Password
                  </label>
                  <input
                    ref={confirmPasswordRef}
                    id="signup-confirm-password"
                    type={showConfirmPassword ? "text" : "password"}
                    autoComplete="new-password"
                    aria-invalid={invalidField === "confirmPassword"}
                    placeholder="••••••••"
                    value={confirmPassword}
                    onChange={(event) => setConfirmPassword(event.target.value)}
                  />
                  <button
                    className={styles.eye}
                    type="button"
                    aria-label={
                      showConfirmPassword
                        ? "Hide confirm password"
                        : "Show confirm password"
                    }
                    onClick={() =>
                      setShowConfirmPassword((visible) => !visible)
                    }
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
                        style={{
                          display: showConfirmPassword ? "block" : "none",
                        }}
                      />
                    </svg>
                  </button>
                </div>
              </div>

              <label className={styles.terms}>
                <input
                  ref={termsRef}
                  type="checkbox"
                  aria-invalid={invalidField === "terms"}
                  checked={acceptedTerms}
                  onChange={(event) => setAcceptedTerms(event.target.checked)}
                />
                <span>
                  By creating an account, you agree to the{" "}
                  <a href="#terms">Terms of Service</a> and{" "}
                  <a href="#privacy">Privacy Policy</a>
                </span>
              </label>

              <div
                className={`${styles.error} ${isSuccess ? styles.success : ""}`}
                role="alert"
                aria-live="polite"
              >
                {error}
              </div>
              <button className={styles.button} type="submit">
                Create Account
              </button>
            </form>
          </div>
        </section>
      </div>
    </main>
  );
}
