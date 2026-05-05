import bash from "highlight.js/lib/languages/bash";
import csharp from "highlight.js/lib/languages/csharp";
import css from "highlight.js/lib/languages/css";
import javascript from "highlight.js/lib/languages/javascript";
import json from "highlight.js/lib/languages/json";
import plaintext from "highlight.js/lib/languages/plaintext";
import sql from "highlight.js/lib/languages/sql";
import typescript from "highlight.js/lib/languages/typescript";
import xml from "highlight.js/lib/languages/xml";
import { createLowlight } from "lowlight";

export const lowlight = createLowlight({
  bash,
  csharp,
  css,
  html: xml,
  javascript,
  json,
  plaintext,
  sql,
  typescript,
});

lowlight.registerAlias({
  bash: ["sh", "shell", "zsh"],
  csharp: ["cs"],
  html: ["xml"],
  javascript: ["js", "jsx"],
  plaintext: ["text", "txt"],
  typescript: ["ts", "tsx"],
});
