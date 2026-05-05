/// <reference types="vite/client" />

import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import "./assets/css/northstar-tokens.css";
import "./assets/css/northstar-login-tokens.css";
import "./assets/css/northstar-layout.css";
import "./assets/css/northstar-components.css";
import "./index.css";
import "./styles/tiptap.css";
import "./assets/css/northstar-login.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
