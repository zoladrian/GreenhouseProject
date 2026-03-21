import { useEffect, useState, type ImgHTMLAttributes } from 'react';

type BrandedImgProps = ImgHTMLAttributes<HTMLImageElement> & {
  /** Gdy główny plik nie istnieje lub się nie wczyta */
  fallbackSrc: string;
  /** Opcjonalny drugi fallback (np. SVG), gdy pierwszy też się nie wczyta */
  fallbackSrc2?: string;
};

/**
 * Obraz marki (PNG/JPG) z łańcuchem fallbacków (np. wariant → poprzedni baner → SVG).
 */
export function BrandedImg({ src, fallbackSrc, fallbackSrc2, onError, ...rest }: BrandedImgProps) {
  const chain = [String(src), fallbackSrc, fallbackSrc2].filter((u): u is string => Boolean(u));
  const deduped = chain.filter((u, i) => chain.indexOf(u) === i);

  const [index, setIndex] = useState(0);

  useEffect(() => {
    setIndex(0);
  }, [src, fallbackSrc, fallbackSrc2]);

  const current = deduped[Math.min(index, deduped.length - 1)] ?? String(src);

  return (
    <img
      {...rest}
      src={current}
      onError={(e) => {
        if (index < deduped.length - 1) {
          setIndex((i) => i + 1);
        }
        onError?.(e);
      }}
    />
  );
}
