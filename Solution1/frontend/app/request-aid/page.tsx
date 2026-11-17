"use client";

import Image from "next/image";
import Link from "next/link";
import { FormEvent, useRef, useState } from "react";
import styles from "./page.module.css";

type Category =
  | "Food"
  | "Medicine"
  | "Shelter"
  | "Transport"
  | "Medical"
  | "Other";

type CreatedRequest = {
  id?: string;
};

const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001"
).replace(/\/+$/, "");

const categories: {
  value: Category;
  description: string;
  path: string;
}[] = [
  {
    value: "Food",
    description: "Meals, water, groceries",
    path: "M3 11h18a9 9 0 0 1-18 0z M8 7c0-1.5 1.5-1.5 1.5-3 M12 7c0-1.5 1.5-1.5 1.5-3 M16 7c0-1.5 1.5-1.5 1.5-3",
  },
  {
    value: "Medicine",
    description: "Prescribed and essential medicines",
    path: "M4 12a5 5 0 0 1 5-5h6a5 5 0 0 1 0 10H9a5 5 0 0 1-5-5z M9 7v10",
  },
  {
    value: "Shelter",
    description: "Temporary housing, tents",
    path: "M3 11l9-7 9 7v9a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1z M9 21v-7h6v7",
  },
  {
    value: "Transport",
    description: "Transportation and evacuation",
    path: "M3 7h12v11H3z M15 11h4l3 3v4h-7z M7 18a2 2 0 1 0 4 0 M17 18a2 2 0 1 0 4 0",
  },
  {
    value: "Medical",
    description: "Medicine, first aid, care",
    path: "M3.5 3.5h17v17h-17z M12 8v8 M8 12h8",
  },
  {
    value: "Other",
    description: "Other essential needs",
    path: "M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18z M12 8v.1 M12 11v5",
  },
];

export default function RequestAidPage() {
  const [category, setCategory] = useState<Category | "">("");
  const [location, setLocation] = useState("");
  const [directions, setDirections] = useState("");
  const [description, setDescription] = useState("");
  const [coordinates, setCoordinates] = useState<{
    latitude: number;
    longitude: number;
  } | null>(null);
  const [categoryError, setCategoryError] = useState("");
  const [locationError, setLocationError] = useState("");
  const [descriptionError, setDescriptionError] = useState("");
  const [submitError, setSubmitError] = useState("");
  const [gpsNote, setGpsNote] = useState("");
  const [requestId, setRequestId] = useState("");
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const firstCategoryRef = useRef<HTMLInputElement>(null);
  const locationRef = useRef<HTMLInputElement>(null);
  const descriptionRef = useRef<HTMLTextAreaElement>(null);

  function handleUseLocation() {
    if (!navigator.geolocation) {
      setGpsNote(
        "Location is not available on this device. Please type the address.",
      );
      return;
    }

    setGpsNote("Finding your location…");
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setCoordinates({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        });
        setLocation((currentLocation) =>
          currentLocation.trim()
            ? currentLocation
            : "My current location",
        );
        setGpsNote("Location captured. Add a landmark if you can.");
      },
      () => {
        setGpsNote(
          "We couldn’t get your location. Please type the address instead.",
        );
      },
      { timeout: 10000 },
    );
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;
    setCategoryError("");
    setLocationError("");
    setDescriptionError("");
    setSubmitError("");

    if (!category) {
      setCategoryError("Choose what you need.");
      firstCategoryRef.current?.focus();
      return;
    }

    const address = [location.trim(), directions.trim()]
      .filter(Boolean)
      .join(", ");
    if (location.trim().length < 4) {
      setLocationError("Enter a delivery location.");
      locationRef.current?.focus();
      return;
    }

    if (address.length > 300) {
      setLocationError("Keep the address and directions within 300 characters.");
      locationRef.current?.focus();
      return;
    }

    if (description.trim().length < 10) {
      setDescriptionError("Describe your situation in at least 10 characters.");
      descriptionRef.current?.focus();
      return;
    }

    const token = window.sessionStorage.getItem("authToken");
    const role = window.sessionStorage.getItem("authRole");
    if (!token || !role) {
      setSubmitError("Please log in before submitting a request.");
      return;
    }
    if (role !== "Citizen") {
      setSubmitError("Only Citizen accounts can submit assistance requests.");
      return;
    }

    const payload = {
      category,
      description: description.trim(),
      address,
      latitude: coordinates?.latitude ?? null,
      longitude: coordinates?.longitude ?? null,
    };

    setIsSubmitting(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/requests`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(payload),
      });

      const responseText = await response.text();
      let responseBody: unknown;
      if (responseText) {
        try {
          responseBody = JSON.parse(responseText);
        } catch {
          if (response.ok) {
            throw new Error("The server returned an invalid request response.");
          }
        }
      }

      if (!response.ok) {
        if (response.status === 401) {
          window.sessionStorage.removeItem("authToken");
          window.sessionStorage.removeItem("authRole");
        }
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
            `Request failed (${response.status} ${response.statusText}).`,
        );
      }

      if (
        !responseBody ||
        typeof responseBody !== "object" ||
        typeof (responseBody as CreatedRequest).id !== "string"
      ) {
        throw new Error("The server response did not include a request ID.");
      }

      setRequestId(`Request #${(responseBody as CreatedRequest).id}`);
      setIsSubmitted(true);
      window.scrollTo({ top: 0, behavior: "smooth" });
    } catch (submitError) {
      setSubmitError(
        submitError instanceof Error
          ? submitError.message
          : "Unable to submit your request. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  function handleSubmitAnother() {
    setCategory("");
    setLocation("");
    setDirections("");
    setDescription("");
    setCoordinates(null);
    setCategoryError("");
    setLocationError("");
    setDescriptionError("");
    setSubmitError("");
    setGpsNote("");
    setRequestId("");
    setIsSubmitted(false);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  return (
    <div className={styles.page}>
      <title>Request Aid · Humanitarian Aid Platform</title>
      <header className={styles.header}>
        <div className={`${styles.wrap} ${styles.nav}`}>
          <Link className={styles.brand} href="/" aria-label="Home">
            <Image
              className={styles.logo}
              src="/humanitarian-aid-platform-logo.svg"
              alt="Humanitarian Aid Platform logo"
              width={50}
              height={50}
            />
            <span>Humanitarian Aid Platform</span>
          </Link>

          <nav className={styles.links} aria-label="Main">
            <Link href="/">Home</Link>
            <Link href="/#requests">Explore Requests</Link>
            <Link href="/#impact">Our Impact</Link>
            <Link href="/#about">About Us</Link>
          </nav>

          <div className={styles.right}>
            <Link
              className={`${styles.button} ${styles.outline} ${styles.small}`}
              href="/login"
            >
              Login
            </Link>
          </div>
        </div>
      </header>

      <main id="request-form">
        <div className={styles.wrap}>
          <div className={styles.top}>
            <div className={styles.crumb}>
              <Link href="/">Home</Link> / Request Aid
            </div>
            <h1>Request Aid</h1>
            <p className={styles.lead}>
              Tell us what you need and where. A coordinator will review your
              request and arrange help as quickly as possible.
            </p>
          </div>

          <div className={styles.layout}>
            <div>
              {!isSubmitted ? (
                <form noValidate onSubmit={handleSubmit}>
                  <section className={styles.card} aria-labelledby="category-title">
                    <div className={styles.sectionHeading}>
                      <span className={styles.number}>1</span>
                      <h2 id="category-title">What do you need?</h2>
                    </div>
                    <p className={styles.hint}>
                      Choose the category that fits best.
                    </p>
                    <fieldset
                      className={styles.categories}
                      aria-describedby="category-error"
                    >
                      <legend className={styles.visuallyHidden}>Category</legend>
                      {categories.map((option, index) => (
                        <label className={styles.categoryCard} key={option.value}>
                          <input
                            ref={index === 0 ? firstCategoryRef : undefined}
                            className={styles.categoryInput}
                            type="radio"
                            name="category"
                            value={option.value}
                            checked={category === option.value}
                            onChange={() => {
                              setCategory(option.value);
                              setCategoryError("");
                            }}
                          />
                          <span className={styles.categoryBox}>
                            <span className={styles.tile}>
                              <svg viewBox="0 0 24 24" aria-hidden="true">
                                <path d={option.path} />
                              </svg>
                            </span>
                            <strong>{option.value}</strong>
                            <small>{option.description}</small>
                          </span>
                          <span className={styles.tick} aria-hidden="true">
                            <svg viewBox="0 0 24 24">
                              <path d="M5 12l5 5 9-10" />
                            </svg>
                          </span>
                        </label>
                      ))}
                    </fieldset>
                    <div
                      className={styles.error}
                      id="category-error"
                      role="alert"
                    >
                      {categoryError}
                    </div>
                  </section>

                  <section className={styles.card} aria-labelledby="location-title">
                    <div className={styles.sectionHeading}>
                      <span className={styles.number}>2</span>
                      <h2 id="location-title">
                        Where should it be delivered?
                      </h2>
                    </div>
                    <p className={styles.hint}>
                      Give an address or a clear landmark so volunteers can find
                      you.
                    </p>
                    <div className={styles.field}>
                      <label className={styles.label} htmlFor="request-location">
                        Delivery location
                      </label>
                      <div className={styles.locationRow}>
                        <input
                          ref={locationRef}
                          className={styles.textInput}
                          id="request-location"
                          type="text"
                          autoComplete="street-address"
                          placeholder="Street, area or landmark"
                          maxLength={300}
                          aria-invalid={Boolean(locationError)}
                          value={location}
                          onChange={(event) => {
                            setLocation(event.target.value);
                            setLocationError("");
                          }}
                        />
                        <button
                          className={styles.gps}
                          type="button"
                          onClick={handleUseLocation}
                        >
                          <svg viewBox="0 0 24 24" aria-hidden="true">
                            <circle cx="12" cy="12" r="3" />
                            <path d="M12 2v4M12 18v4M2 12h4M18 12h4" />
                          </svg>
                          Use my location
                        </button>
                      </div>
                      <div className={styles.error} role="alert">
                        {locationError}
                      </div>
                      <div className={styles.note} aria-live="polite">
                        {gpsNote}
                      </div>
                    </div>
                    <div className={styles.field}>
                      <label className={styles.label} htmlFor="request-directions">
                        Extra directions <span className={styles.optional}>(optional)</span>
                      </label>
                      <input
                        className={styles.textInput}
                        id="request-directions"
                        type="text"
                        maxLength={300}
                        placeholder="Gate colour, nearby building, access road…"
                        value={directions}
                        onChange={(event) => setDirections(event.target.value)}
                      />
                    </div>
                  </section>

                  <section className={styles.card} aria-labelledby="description-title">
                    <div className={styles.sectionHeading}>
                      <span className={styles.number}>3</span>
                      <h2 id="description-title">Describe your situation</h2>
                    </div>
                    <p className={styles.hint}>
                      Include what happened and any specific needs.
                    </p>
                    <div className={styles.field}>
                      <label className={styles.label} htmlFor="request-description">
                        What happened?
                      </label>
                      <textarea
                        ref={descriptionRef}
                        className={styles.textarea}
                        id="request-description"
                        minLength={10}
                        maxLength={2000}
                        placeholder="Describe the situation and anything specific you need, such as baby formula or insulin."
                        value={description}
                        aria-invalid={Boolean(descriptionError)}
                        onChange={(event) => {
                          setDescription(event.target.value);
                          setDescriptionError("");
                        }}
                      />
                      <div className={styles.error} role="alert">
                        {descriptionError}
                      </div>
                      <div className={styles.note}>
                        {description.length}/2000
                      </div>
                    </div>
                  </section>

                  <div className={`${styles.card} ${styles.submitPanel}`}>
                    {submitError && (
                      <div className={styles.error} role="alert">
                        {submitError}
                      </div>
                    )}
                    <button
                      className={`${styles.button} ${styles.solid}`}
                      type="submit"
                      disabled={isSubmitting}
                    >
                      {isSubmitting ? "Submitting..." : "Submit request"}
                    </button>
                    <p>
                      By submitting, you agree that coordinators may review
                      your request and contact you if more information is needed.
                    </p>
                  </div>
                </form>
              ) : (
                <section className={`${styles.card} ${styles.done}`} aria-live="polite">
                  <div className={styles.successIcon}>
                    <svg viewBox="0 0 24 24" aria-hidden="true">
                      <path d="M5 12l5 5 9-10" />
                    </svg>
                  </div>
                  <h2>Request received</h2>
                  <div className={styles.requestId}>{requestId}</div>
                  <p>
                    Thank you. Your request is now in the coordinator queue.
                  </p>
                  <div className={styles.doneActions}>
                    <button
                      className={`${styles.button} ${styles.solid}`}
                      type="button"
                      onClick={handleSubmitAnother}
                    >
                      Submit another request
                    </button>
                    <Link
                      className={`${styles.button} ${styles.outline}`}
                      href="/"
                    >
                      Back to home
                    </Link>
                  </div>
                </section>
              )}
            </div>

            <aside className={styles.side} aria-label="Request summary">
              <div className={`${styles.card} ${styles.sideCard}`}>
                <h3>Your request</h3>
                <dl className={styles.summary}>
                  <div>
                    <dt>Category</dt>
                    <dd>{category || "Not chosen"}</dd>
                  </div>
                  <div>
                    <dt>Location</dt>
                    <dd>{location.trim() || "Not entered"}</dd>
                  </div>
                </dl>
              </div>
              <div className={`${styles.card} ${styles.sideCard}`}>
                <div className={styles.art}>
                  <Image
                    src="/signup-community.jpg"
                    alt="Volunteers carrying supplies and planting a tree together"
                    width={940}
                    height={640}
                    sizes="(max-width: 980px) 0px, 340px"
                  />
                </div>
                <h3>What happens next</h3>
                <ol className={styles.steps}>
                  <li>
                    <b>1</b>
                    <span>A coordinator reviews your request details.</span>
                  </li>
                  <li>
                    <b>2</b>
                    <span>
                      A coordinator matches shelters, stock and volunteers.
                    </span>
                  </li>
                  <li>
                    <b>3</b>
                    <span>The team coordinates the next steps with you.</span>
                  </li>
                </ol>
              </div>
              <div className={styles.sos}>
                <strong>In immediate danger?</strong> Contact your local
                emergency services first, then submit this form.
              </div>
            </aside>
          </div>
        </div>
      </main>
    </div>
  );
}
