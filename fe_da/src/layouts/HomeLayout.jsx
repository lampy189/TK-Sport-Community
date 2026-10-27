import Header from "../components/layout/Header";
import Navbar from "../components/layout/Navbar";
import HeroLayout from "../components/layout/HeroLayout";
import MainContainer from "../components/layout/MainContainer";
import Footer from "../components/layout/Footer";
import "../styles/home.css";

// Each page composes SectionLayout instances inside children.
export default function HomeLayout({
  roleLabel,
  accountName,
  onLogout,
  onLogin,
  onHome,
  role = "guest",
  navigation,
  activeId,
  onNavigate,
  hero,
  children,
}) {
  return (
    <div id="home" className="home-layout" data-role={role}>
      <a className="skip-link" href="#main-content">
        Bỏ qua điều hướng
      </a>
      <Header roleLabel={roleLabel} accountName={accountName} onLogout={onLogout} onLogin={onLogin} onHome={onHome} />
      <Navbar items={navigation} activeId={activeId} onNavigate={onNavigate} />
      <MainContainer>
        {hero && <HeroLayout {...hero} />}
        {children}
      </MainContainer>
      <Footer />
    </div>
  );
}
