import badmintonPhoto from "../assets/session-photos/badminton.webp";
import badmintonPhuThoPhoto from "../assets/session-photos/badminton-phu-tho.webp";
import badmintonCtaPhoto from "../assets/session-photos/badminton-cta.webp";
import pickleballPhoto from "../assets/session-photos/pickleball.webp";
import shinkoAislePhoto from "../assets/session-photos/pickleball-shinko-aisle.webp";
import shinkoCourtsPhoto from "../assets/session-photos/pickleball-shinko-courts.webp";

const badmintonPhotos = [badmintonPhuThoPhoto, badmintonPhoto, badmintonCtaPhoto];
const shinkoPhotos = [shinkoCourtsPhoto, shinkoAislePhoto];

export default function SessionCourtPhoto({ session, detail = false }) {
  const sport = session.sportCode?.toLowerCase() || session.sportName?.toLowerCase();
  const courtId = Number(session.courtId);
  const hasCourt = Number.isInteger(courtId) && courtId > 0;
  const isShinkoPhoto = sport === "pickleball" && hasCourt;
  const photos = sport === "badminton" ? badmintonPhotos : isShinkoPhoto ? shinkoPhotos : null;
  const photo = photos
    ? hasCourt ? photos[courtId % photos.length] : badmintonPhoto
    : sport === "pickleball" ? pickleballPhoto : null;
  const alt = isShinkoPhoto
    ? "Toàn cảnh khu sân pickleball Shinko"
    : sport === "badminton" ? "Toàn cảnh sân cầu lông trong nhà" : "Toàn cảnh sân pickleball ngoài trời";

  if (!photo) return null;

  return (
    <figure className={detail ? "session-photo session-photo-detail" : "session-photo"}>
      <img
        src={photo}
        alt={detail ? alt : ""}
        className={isShinkoPhoto ? "session-photo-courts" : undefined}
        loading={detail ? "eager" : "lazy"}
        decoding="async"
      />
    </figure>
  );
}
