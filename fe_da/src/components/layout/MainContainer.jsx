export default function MainContainer({ children }) {
  return (
    <main id="main-content" className="container main-container" tabIndex={-1}>
      {children}
    </main>
  );
}
