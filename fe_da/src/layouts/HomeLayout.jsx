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
  navigation,
  activeId,
  onNavigate,
  hero,
  children,
}) {
  return (
    <div id="home" className="home-layout">
      <a className="skip-link" href="#main-content">
        Bỏ qua điều hướng
      </a>
      <Header roleLabel={roleLabel} accountName={accountName} />
      <Navbar items={navigation} activeId={activeId} onNavigate={onNavigate} />
      <MainContainer>
        <HeroLayout {...hero} />
        {children}
      </MainContainer>
      <Footer />
    </div>
  );
}
