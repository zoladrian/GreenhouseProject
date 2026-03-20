import { useEffect, useState, type ImgHTMLAttributes } from 'react';

type BrandedImgProps = ImgHTMLAttributes<HTMLImageElement> & {
  /** Gdy główny plik nie istnieje lub się nie wczyta */
  fallbackSrc: string;
};

/**
 * Obraz marki (PNG/JPG) z bezpiecznym fallbackiem do SVG z repozytorium.
 */
export function BrandedImg({ src, fallbackSrc, onError, ...rest }: BrandedImgProps) {
  const [current, setCurrent] = useState(() => String(src));

  useEffect(() => {
    setCurrent(String(src));
  }, [src]);

  return (
    <img
      {...rest}
      src={current}
      onError={(e) => {
        if (current !== fallbackSrc) {
          setCurrent(fallbackSrc);
        }
        onError?.(e);
      }}
    />
  );
}
