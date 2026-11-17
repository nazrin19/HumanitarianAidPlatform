"use client";

import Image from "next/image";
import Link from "next/link";
import {
  ChangeEvent,
  CSSProperties,
  DragEvent,
  FormEvent,
  useEffect,
  useRef,
  useState,
} from "react";
import styles from "./page.module.css";

type Category = "Food" | "Clothing" | "Shelter" | "Medical";

type UploadedFile = {
  file: File;
  url: string;
};

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
    value: "Clothing",
    description: "Clothes, blankets, hygiene",
    path: "M8 3 3 6l2 4 3-1v12h8V9l3 1 2-4-5-3a4 4 0 0 1-8 0z",
  },
  {
    value: "Shelter",
    description: "Temporary housing, tents",
    path: "M3 11l9-7 9 7v9a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1z M9 21v-7h6v7",
  },
  {
    value: "Medical",
    description: "Medicine, first aid, care",
    path: "M3.5 3.5h17v17h-17z M12 8v8 M8 12h8",
  },
];

const urgencyLevels = [
  {
    value: 1,
    name: "Low",
    color: "#1b7f4b",
    background: "#ddf3e5",
    description: "Can wait a few days.",
  },
  {
    value: 2,
    name: "Medium",
    color: "#b25e09",
    background: "#fff1d6",
    description: "Needed within 24–48 hours.",
  },
  {
    value: 3,
    name: "High",
    color: "#c2410c",
    background: "#ffe4d6",
    description: "Needed within about 12 hours.",
  },
  {
    value: 4,
    name: "Critical",
    color: "#a31d1d",
    background: "#fde8e8",
    description: "Immediate risk to life or safety.",
  },
] as const;

const MAX_FILES = 5;
const MAX_FILE_SIZE = 10 * 1024 * 1024;

function formatFileSize(bytes: number) {
  return bytes > 1048576
    ? `${(bytes / 1048576).toFixed(1)} MB`
    : `${Math.max(1, Math.round(bytes / 1024))} KB`;
}

export default function RequestAidPage() {
  const [category, setCategory] = useState<Category | "">("");
  const [urgency, setUrgency] = useState(2);
  const [location, setLocation] = useState("");
  const [directions, setDirections] = useState("");
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [peopleAffected, setPeopleAffected] = useState("");
  const [description, setDescription] = useState("");
  const [attachments, setAttachments] = useState<UploadedFile[]>([]);
  const [categoryError, setCategoryError] = useState("");
  const [locationError, setLocationError] = useState("");
  const [nameError, setNameError] = useState("");
  const [phoneError, setPhoneError] = useState("");
  const [fileError, setFileError] = useState("");
  const [gpsNote, setGpsNote] = useState("");
  const [isDragging, setIsDragging] = useState(false);
  const [requestId, setRequestId] = useState("");
  const [isSubmitted, setIsSubmitted] = useState(false);
  const attachmentsRef = useRef<UploadedFile[]>([]);
  const firstCategoryRef = useRef<HTMLInputElement>(null);
  const locationRef = useRef<HTMLInputElement>(null);
  const nameRef = useRef<HTMLInputElement>(null);
  const phoneRef = useRef<HTMLInputElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const urgencyInfo = urgencyLevels[urgency - 1];

  useEffect(
    () => () => {
      attachmentsRef.current.forEach((attachment) =>
        URL.revokeObjectURL(attachment.url),
      );
    },
    [],
  );

  function updateAttachments(nextAttachments: UploadedFile[]) {
    attachmentsRef.current = nextAttachments;
    setAttachments(nextAttachments);
  }

  function addFiles(fileList: FileList | File[]) {
    const nextAttachments = [...attachments];
    const errors: string[] = [];

    Array.from(fileList).forEach((file) => {
      if (!file.type.startsWith("image/") && file.type !== "application/pdf") {
        errors.push(`${file.name} isn’t a photo or PDF.`);
        return;
      }

      if (file.size > MAX_FILE_SIZE) {
        errors.push(`${file.name} is larger than 10 MB.`);
        return;
      }

      if (nextAttachments.length >= MAX_FILES) {
        errors.push("You can attach up to 5 files.");
        return;
      }

      if (
        nextAttachments.some(
          (attachment) =>
            attachment.file.name === file.name &&
            attachment.file.size === file.size,
        )
      ) {
        return;
      }

      nextAttachments.push({ file, url: URL.createObjectURL(file) });
    });

    updateAttachments(nextAttachments);
    setFileError([...new Set(errors)].join(" "));
  }

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    if (event.target.files) {
      addFiles(event.target.files);
    }

    event.target.value = "";
  }

  function handleDrop(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault();
    setIsDragging(false);
    addFiles(event.dataTransfer.files);
  }

  function removeFile(url: string) {
    const removed = attachments.find((attachment) => attachment.url === url);
    if (!removed) {
      return;
    }

    URL.revokeObjectURL(removed.url);
    updateAttachments(
      attachments.filter((attachment) => attachment.url !== removed.url),
    );
  }

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
        const coordinates = `${position.coords.latitude.toFixed(5)}, ${position.coords.longitude.toFixed(5)}`;
        setLocation((currentLocation) =>
          currentLocation.trim()
            ? currentLocation
            : `My current location (${coordinates})`,
        );
        setGpsNote("Location captured. Add a landmark above if you can.");
      },
      () => {
        setGpsNote(
          "We couldn’t get your location. Please type the address instead.",
        );
      },
      { timeout: 10000 },
    );
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setCategoryError("");
    setLocationError("");
    setNameError("");
    setPhoneError("");

    if (!category) {
      setCategoryError("Choose what you need.");
      firstCategoryRef.current?.focus();
      return;
    }

    if (location.trim().length < 4) {
      setLocationError("Enter a delivery location.");
      locationRef.current?.focus();
      return;
    }

    if (name.trim().length < 2) {
      setNameError("Enter your name.");
      nameRef.current?.focus();
      return;
    }

    if (!/^\+?[\d\s-]{7,}$/.test(phone.trim())) {
      setPhoneError("Enter a valid phone number.");
      phoneRef.current?.focus();
      return;
    }

    setRequestId(`Request #REQ-${1043 + Math.floor(Math.random() * 900)}`);
    setIsSubmitted(true);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleSubmitAnother() {
    attachments.forEach((attachment) =>
      URL.revokeObjectURL(attachment.url),
    );
    updateAttachments([]);
    setCategory("");
    setUrgency(2);
    setLocation("");
    setDirections("");
    setName("");
    setPhone("");
    setPeopleAffected("");
    setDescription("");
    setCategoryError("");
    setLocationError("");
    setNameError("");
    setPhoneError("");
    setFileError("");
    setGpsNote("");
    setRequestId("");
    setIsSubmitted(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
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
              request, check its urgency and arrange help as quickly as possible.
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

                  <section className={styles.card} aria-labelledby="urgency-title">
                    <div className={styles.sectionHeading}>
                      <span className={styles.number}>2</span>
                      <h2 id="urgency-title">How urgent is it?</h2>
                    </div>
                    <p className={styles.hint}>
                      Drag the slider. Coordinators review the most urgent
                      requests first.
                    </p>
                    <div className={styles.urgency}>
                      <span
                        className={styles.urgencyPill}
                        style={{
                          background: urgencyInfo.background,
                          color: urgencyInfo.color,
                        }}
                      >
                        {urgencyInfo.name}
                      </span>
                      <span className={styles.urgencyDescription}>
                        {urgencyInfo.description}
                      </span>
                    </div>
                    <input
                      className={styles.range}
                      type="range"
                      min="1"
                      max="4"
                      step="1"
                      value={urgency}
                      aria-label="Urgency level"
                      aria-valuetext={urgencyInfo.name}
                      style={
                        { "--range-color": urgencyInfo.color } as CSSProperties
                      }
                      onChange={(event) => setUrgency(Number(event.target.value))}
                    />
                    <div className={styles.scale}>
                      {urgencyLevels.map((level) => (
                        <button
                          className={
                            urgency === level.value ? styles.activeScale : ""
                          }
                          key={level.value}
                          type="button"
                          aria-pressed={urgency === level.value}
                          onClick={() => setUrgency(level.value)}
                        >
                          {level.name}
                        </button>
                      ))}
                    </div>
                  </section>

                  <section className={styles.card} aria-labelledby="location-title">
                    <div className={styles.sectionHeading}>
                      <span className={styles.number}>3</span>
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
                        placeholder="Gate colour, nearby building, access road…"
                        value={directions}
                        onChange={(event) => setDirections(event.target.value)}
                      />
                    </div>
                  </section>

                  <section className={styles.card} aria-labelledby="verify-title">
                    <div className={styles.sectionHeading}>
                      <span className={styles.number}>4</span>
                      <h2 id="verify-title">Verify the situation</h2>
                    </div>
                    <p className={styles.hint}>
                      Photos or documents of the affected site help us verify
                      and prioritise your request. Optional, but recommended.
                    </p>
                    <label
                      className={`${styles.drop} ${isDragging ? styles.over : ""}`}
                      htmlFor="request-files"
                      onDragEnter={(event) => {
                        event.preventDefault();
                        setIsDragging(true);
                      }}
                      onDragOver={(event) => event.preventDefault()}
                      onDragLeave={(event) => {
                        event.preventDefault();
                        setIsDragging(false);
                      }}
                      onDrop={handleDrop}
                    >
                      <input
                        ref={fileInputRef}
                        className={styles.fileInput}
                        id="request-files"
                        type="file"
                        multiple
                        accept="image/*,application/pdf"
                        onChange={handleFileChange}
                      />
                      <span className={`${styles.tile} ${styles.dropTile}`}>
                        <svg viewBox="0 0 24 24" aria-hidden="true">
                          <path d="M12 16V4M7 9l5-5 5 5" />
                          <path d="M4 16v3a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-3" />
                        </svg>
                      </span>
                      <strong>
                        Drag photos here or <u>browse files</u>
                      </strong>
                      <span>JPG, PNG or PDF · up to 5 files · 10 MB each</span>
                    </label>
                    <div className={styles.error} role="alert">
                      {fileError}
                    </div>
                    {attachments.length > 0 && (
                      <ul className={styles.files} aria-label="Attached files">
                        {attachments.map((attachment) => (
                          <li key={attachment.url}>
                            {attachment.file.type.startsWith("image/") ? (
                              <Image
                                className={styles.thumbnail}
                                src={attachment.url}
                                alt=""
                                width={48}
                                height={48}
                                unoptimized
                              />
                            ) : (
                              <div className={styles.thumbnail}>PDF</div>
                            )}
                            <div className={styles.fileName}>
                              {attachment.file.name}
                              <small>
                                {formatFileSize(attachment.file.size)}
                              </small>
                            </div>
                            <button
                              type="button"
                              aria-label={`Remove ${attachment.file.name}`}
                              onClick={() => removeFile(attachment.url)}
                            >
                              ×
                            </button>
                          </li>
                        ))}
                      </ul>
                    )}
                  </section>

                  <section className={styles.card} aria-labelledby="contact-title">
                    <div className={styles.sectionHeading}>
                      <span className={styles.number}>5</span>
                      <h2 id="contact-title">Details and contact</h2>
                    </div>
                    <p className={styles.hint}>
                      So we can reach you if we need more information.
                    </p>
                    <div className={styles.gridTwo}>
                      <div className={styles.field}>
                        <label className={styles.label} htmlFor="request-name">
                          Your name
                        </label>
                        <input
                          ref={nameRef}
                          className={styles.textInput}
                          id="request-name"
                          type="text"
                          autoComplete="name"
                          placeholder="Full name"
                          aria-invalid={Boolean(nameError)}
                          value={name}
                          onChange={(event) => {
                            setName(event.target.value);
                            setNameError("");
                          }}
                        />
                        <div className={styles.error} role="alert">
                          {nameError}
                        </div>
                      </div>
                      <div className={styles.field}>
                        <label className={styles.label} htmlFor="request-phone">
                          Phone number
                        </label>
                        <input
                          ref={phoneRef}
                          className={styles.textInput}
                          id="request-phone"
                          type="tel"
                          autoComplete="tel"
                          placeholder="+94 7X XXX XXXX"
                          aria-invalid={Boolean(phoneError)}
                          value={phone}
                          onChange={(event) => {
                            setPhone(event.target.value);
                            setPhoneError("");
                          }}
                        />
                        <div className={styles.error} role="alert">
                          {phoneError}
                        </div>
                      </div>
                    </div>
                    <div className={styles.field}>
                      <label className={styles.label} htmlFor="request-people">
                        People affected{" "}
                        <span className={styles.optional}>(optional)</span>
                      </label>
                      <input
                        className={styles.textInput}
                        id="request-people"
                        type="number"
                        min="1"
                        max="10000"
                        placeholder="e.g. 4"
                        value={peopleAffected}
                        onChange={(event) =>
                          setPeopleAffected(event.target.value)
                        }
                      />
                    </div>
                    <div className={styles.field}>
                      <label className={styles.label} htmlFor="request-description">
                        What happened?{" "}
                        <span className={styles.optional}>(optional)</span>
                      </label>
                      <textarea
                        className={styles.textarea}
                        id="request-description"
                        maxLength={400}
                        placeholder="Describe the situation and anything specific you need, such as baby formula or insulin."
                        value={description}
                        onChange={(event) =>
                          setDescription(event.target.value)
                        }
                      />
                      <div className={styles.note}>
                        {description.length}/400
                      </div>
                    </div>
                  </section>

                  <div className={`${styles.card} ${styles.submitPanel}`}>
                    <button
                      className={`${styles.button} ${styles.solid}`}
                      type="submit"
                    >
                      Submit request
                    </button>
                    <p>
                      By submitting, you agree that coordinators may contact you
                      about this request.
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
                    Thank you. Your request is now in the coordinator queue, and
                    someone will contact you on the number you gave. Keep your
                    phone nearby.
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
                    <dt>Urgency</dt>
                    <dd style={{ color: urgencyInfo.color }}>
                      {urgencyInfo.name}
                    </dd>
                  </div>
                  <div>
                    <dt>Location</dt>
                    <dd>{location.trim() || "Not entered"}</dd>
                  </div>
                  <div>
                    <dt>Photos</dt>
                    <dd>
                      {attachments.length
                        ? `${attachments.length} attached`
                        : "None"}
                    </dd>
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
                    <span>We check the urgency and your photos.</span>
                  </li>
                  <li>
                    <b>2</b>
                    <span>
                      A coordinator matches shelters, stock and volunteers.
                    </span>
                  </li>
                  <li>
                    <b>3</b>
                    <span>You get an SMS or call with the dispatch details.</span>
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
