import { useState } from "react";
import { ApiClientError, getConfiguredApiBaseUrl } from "../lib/apiClient";
import { login, register } from "../lib/authClient";
import { getPostLoginRedirectHash } from "../lib/hashRouting";
import compassRoseEmblemUrl from "../assets/svg/brand/compass-rose-emblem.svg";
import smallCompassMarkUrl from "../assets/svg/brand/small-compass-mark.svg";
import cardCornerDetailUrl from "../assets/svg/decorative/card-corner-detail.svg";
import coordinateFooterMarksUrl from "../assets/svg/decorative/coordinate-footer-marks.svg";
import coordinateTickModuleUrl from "../assets/svg/decorative/coordinate-tick-module.svg";
import crosshairLocatorMarksUrl from "../assets/svg/decorative/crosshair-locator-marks.svg";
import dashedRouteLinesUrl from "../assets/svg/decorative/dashed-route-lines.svg";
import editorialDividerLineUrl from "../assets/svg/decorative/editorial-divider-line.svg";
import orientationDotSetUrl from "../assets/svg/decorative/orientation-dot-set.svg";
import emailIconUrl from "../assets/svg/icons/email.svg";
import externalLinkIconUrl from "../assets/svg/icons/external-link.svg";
import eyeIconUrl from "../assets/svg/icons/eye.svg";
import lockIconUrl from "../assets/svg/icons/lock.svg";
import mapPinWaypointUrl from "../assets/svg/icons/map-pin-waypoint.svg";
import shieldCheckIconUrl from "../assets/svg/icons/shield-check.svg";
import loginLeftMapOverlayUrl from "../assets/svg/patterns/login-left-map-overlay.svg";
import topographicContourPatchUrl from "../assets/svg/patterns/topographic-contour-patch.svg";

export function NorthstarLoginPage() {
  const [authMode, setAuthMode] = useState<"login" | "register">("login");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [password, setPassword] = useState("");
  const [status, setStatus] = useState<"idle" | "submitting" | "success" | "error">("idle");
  const apiConfigured = Boolean(getConfiguredApiBaseUrl());

  const submit = async () => {
    if (!apiConfigured) {
      setStatus("error");
      setMessage("API base URL is not configured.");
      return;
    }

    const validationError = validateAuthForm({
      authMode,
      confirmPassword,
      displayName,
      email,
      password,
    });
    if (validationError) {
      setStatus("error");
      setMessage(validationError);
      return;
    }

    setStatus("submitting");
    setMessage(null);

    try {
      if (authMode === "register") {
        await register({
          displayName: displayName.trim(),
          email: email.trim(),
          password,
        });
      } else {
        await login({ email: email.trim(), password });
      }

      setStatus("success");
      setMessage(authMode === "register" ? "Account created." : "Signed in.");
      window.location.hash = getPostLoginRedirectHash(window.location.hash);
    } catch (error) {
      setStatus("error");
      setMessage(getAuthErrorMessage(error));
    }
  };

  const switchMode = (nextMode: "login" | "register") => {
    setAuthMode(nextMode);
    setMessage(null);
    setStatus("idle");
  };

  const isRegisterMode = authMode === "register";
  const submitLabel = isRegisterMode ? "Create account" : "Sign in";
  const submittingLabel = isRegisterMode ? "Creating account" : "Signing in";

  return (
    <div className="login-page">
      <aside className="login-brand-panel" aria-label="Northstar brand panel">
        <img className="left-map-overlay" src={loginLeftMapOverlayUrl} alt="" />
        <img className="brand-route-lines" src={dashedRouteLinesUrl} alt="" />
        <img className="map-pin-waypoint" src={mapPinWaypointUrl} alt="" />

        <div className="brand-lockup" aria-label="Northstar">
          <div className="brand-main-compass" aria-hidden="true">
            <span className="brand-north-marker" />
            <span className="brand-north-label">N</span>
            <img src={compassRoseEmblemUrl} alt="" />
          </div>
          <div className="brand-wordmark">Northstar</div>
          <img className="brand-divider" src={editorialDividerLineUrl} alt="" />
          <p className="brand-tagline">Map knowledge. Find direction.</p>
        </div>

        <img className="orientation-dots" src={orientationDotSetUrl} alt="" />
        <div className="brand-coordinate-caption">Section A-03 - Coordinates 47.61 / 122.33</div>
      </aside>

      <main className="login-form-area">
        <div className="coordinate-label-top" aria-hidden="true">
          <img src={coordinateTickModuleUrl} alt="" />
          <span>47.61 N&nbsp;&nbsp;122.33 W</span>
        </div>

        <img className="topographic-patch" src={topographicContourPatchUrl} alt="" />
        <img className="locator-marks" src={crosshairLocatorMarksUrl} alt="" />
        <img className="login-small-compass-bg" src={smallCompassMarkUrl} alt="" />

        <section className="login-card" aria-labelledby="login-title">
          {["top-left", "top-right", "bottom-right", "bottom-left"].map((position) => (
            <img key={position} className={`card-corner ${position}`} src={cardCornerDetailUrl} alt="" />
          ))}

          <form
            className="login-card-body"
            onSubmit={(event) => {
              event.preventDefault();
              void submit();
            }}
          >
            <div className="login-heading">
              <h1 id="login-title">{isRegisterMode ? "Create your Northstar account" : "Sign in to Northstar"}</h1>
              <p>{isRegisterMode ? "Start with a protected workspace identity." : "Access your workspace and documents."}</p>
            </div>

            <div className="auth-mode-switch" aria-label="Authentication mode">
              <button
                aria-pressed={!isRegisterMode}
                className={!isRegisterMode ? "is-active" : ""}
                onClick={() => switchMode("login")}
                type="button"
              >
                Sign in
              </button>
              <button
                aria-pressed={isRegisterMode}
                className={isRegisterMode ? "is-active" : ""}
                onClick={() => switchMode("register")}
                type="button"
              >
                Register
              </button>
            </div>

            {isRegisterMode ? (
              <>
                <label className="field-label" htmlFor="northstar-display-name">
                  Display name
                </label>
                <div className="field-control">
                  <input
                    id="northstar-display-name"
                    name="displayName"
                    type="text"
                    autoComplete="name"
                    onChange={(event) => setDisplayName(event.target.value)}
                    placeholder="Your name"
                    value={displayName}
                  />
                </div>
              </>
            ) : null}

            <label className="field-label" htmlFor="northstar-email">
              Email
            </label>
            <div className="field-control">
              <img className="field-icon" src={emailIconUrl} alt="" />
              <input
                id="northstar-email"
                name="email"
                type="email"
                autoComplete="email"
                onChange={(event) => setEmail(event.target.value)}
                placeholder="you@example.com"
                value={email}
              />
            </div>

            <label className="field-label" htmlFor="northstar-password">
              Password
            </label>
            <div className="field-control">
              <img className="field-icon" src={lockIconUrl} alt="" />
              <input
                id="northstar-password"
                name="password"
                type={isPasswordVisible ? "text" : "password"}
                autoComplete={isRegisterMode ? "new-password" : "current-password"}
                onChange={(event) => setPassword(event.target.value)}
                placeholder="********"
                value={password}
              />
              <button
                className="password-eye"
                type="button"
                aria-label={isPasswordVisible ? "Hide password" : "Show password"}
                onClick={() => setIsPasswordVisible((currentValue) => !currentValue)}
              >
                <img src={eyeIconUrl} alt="" />
              </button>
            </div>

            {isRegisterMode ? (
              <>
                <label className="field-label" htmlFor="northstar-confirm-password">
                  Confirm password
                </label>
                <div className="field-control">
                  <img className="field-icon" src={lockIconUrl} alt="" />
                  <input
                    id="northstar-confirm-password"
                    name="confirmPassword"
                    type={isPasswordVisible ? "text" : "password"}
                    autoComplete="new-password"
                    onChange={(event) => setConfirmPassword(event.target.value)}
                    placeholder="********"
                    value={confirmPassword}
                  />
                </div>
              </>
            ) : null}

            {message ? (
              <div className={["login-inline-status", status === "error" ? "is-error" : ""].join(" ")}>{message}</div>
            ) : null}

            <button className="sign-in-button" disabled={status === "submitting"} type="submit">
              {status === "submitting" ? submittingLabel : submitLabel}
            </button>

            <div className="login-divider" aria-hidden="true">
              <span />
              <em>or</em>
              <span />
            </div>

            <button className="sso-button" disabled title="OIDC/SAML login UI is deferred" type="button">
              <span className="google-mark" aria-hidden="true">
                G
              </span>
              <span>Continue with Google</span>
            </button>
            <button className="sso-button" disabled title="OIDC/SAML login UI is deferred" type="button">
              <span className="microsoft-mark" aria-hidden="true">
                <span />
                <span />
                <span />
                <span />
              </span>
              <span>Continue with Microsoft</span>
            </button>

            <div className="login-links">
              {isRegisterMode ? (
                <button className="login-link-button" onClick={() => switchMode("login")} type="button">
                  Already have an account
                </button>
              ) : (
                <a href="#forgot-password">Forgot password</a>
              )}
              <a href="#workspace-url">
                <span>Access via workspace URL</span>
                <img src={externalLinkIconUrl} alt="" />
              </a>
            </div>
          </form>

          <footer className="login-card-footer">
            <img src={shieldCheckIconUrl} alt="" />
            <span>Protected workspace access</span>
          </footer>
        </section>

        <img className="coordinate-footer-marks" src={coordinateFooterMarksUrl} alt="" />
      </main>
    </div>
  );
}

function validateAuthForm(values: {
  authMode: "login" | "register";
  confirmPassword: string;
  displayName: string;
  email: string;
  password: string;
}) {
  if (values.authMode === "register" && !values.displayName.trim()) {
    return "Display name is required.";
  }

  if (!values.email.trim() || !values.password) {
    return "Email and password are required.";
  }

  if (values.authMode === "register" && values.password.length < 8) {
    return "Password must be at least 8 characters.";
  }

  if (values.authMode === "register" && values.password !== values.confirmPassword) {
    return "Passwords must match.";
  }

  return null;
}

function getAuthErrorMessage(error: unknown) {
  if (!(error instanceof ApiClientError)) {
    return "Could not complete authentication.";
  }

  if (error.status === 401) {
    return "Invalid email or password.";
  }

  if (error.status === 400 || error.code === "VALIDATION_ERROR") {
    return error.message || "Request validation failed.";
  }

  if (error.status === 0) {
    return "Could not reach the API. Check that the backend and dev server are running.";
  }

  return error.message || "Could not complete authentication.";
}
