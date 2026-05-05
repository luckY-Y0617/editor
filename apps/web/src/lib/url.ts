type NormalizeUrlOptions = {
  allowedSchemes?: string[];
  defaultScheme?: "https";
};

export function normalizeUrl(value: string, options: NormalizeUrlOptions = {}) {
  const { allowedSchemes = ["http", "https"], defaultScheme = "https" } = options;
  const trimmedValue = value.trim();

  if (!trimmedValue) {
    return "";
  }

  const explicitScheme = trimmedValue.match(/^([a-z][a-z0-9+.-]*):/i)?.[1]?.toLowerCase();

  if (explicitScheme) {
    return allowedSchemes.includes(explicitScheme) ? trimmedValue : "";
  }

  return `${defaultScheme}://${trimmedValue}`;
}
