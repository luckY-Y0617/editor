import type { CSSProperties } from "react";

type AtlasIconProps = {
  src: string;
  className?: string;
  label?: string;
};

export function AtlasIcon({ className = "", label, src }: AtlasIconProps) {
  const maskStyle: CSSProperties = {
    WebkitMaskImage: `url(${src})`,
    WebkitMaskPosition: "center",
    WebkitMaskRepeat: "no-repeat",
    WebkitMaskSize: "contain",
    maskImage: `url(${src})`,
    maskPosition: "center",
    maskRepeat: "no-repeat",
    maskSize: "contain",
  };

  return (
    <span
      aria-hidden={label ? undefined : true}
      aria-label={label}
      className={`inline-block shrink-0 bg-current ${className}`}
      role={label ? "img" : undefined}
      style={maskStyle}
    />
  );
}
