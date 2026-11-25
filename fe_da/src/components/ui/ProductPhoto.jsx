import { Package } from 'lucide-react';
import { useState } from 'react';
import water from '../../assets/products/water.webp';
import shuttle from '../../assets/products/shuttle.webp';
import ball from '../../assets/products/ball.webp';
import towel from '../../assets/products/towel.webp';
import racket from '../../assets/products/racket.webp';
import socks from '../../assets/products/socks.webp';
import granola from '../../assets/products/granola.webp';
import './productPhoto.css';

const photos = new Map(Object.entries({
  WATER: water,
  SHUTTLE: shuttle,
  BALL: ball,
  TOWEL: towel,
  'BADMINTON-RACKET': racket,
  'SPORT-SOCKS': socks,
  'GRANOLA-BAR': granola,
}));

function PhotoImage({ source, fallback }) {
  const [failed, setFailed] = useState([]);
  const src = [source, fallback].find(value => value && !failed.includes(value));
  return src
    ? <img src={src} alt="" loading="lazy" decoding="async" onError={() => setFailed(previous => [...previous, src])} />
    : <Package />;
}

export default function ProductPhoto({ sku, productId, large = false, revision = 0, previewUrl }) {
  const fallback = photos.get(sku?.trim().toUpperCase().replace(/^(VP|HN)-/, ''));
  const source = previewUrl || (productId ? `/api/products/${productId}/image?v=${revision}` : undefined);
  return (
    <span className={large ? 'product-photo product-photo-large' : 'product-photo'} aria-hidden="true">
      <PhotoImage key={source || fallback} source={source} fallback={fallback} />
    </span>
  );
}
