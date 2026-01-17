import Hero from "@/components/sections/Hero";
import Visualization from "@/components/sections/Visualization";
import PainPoint from "@/components/sections/PainPoint";
import HowItWorks from "@/components/sections/HowItWorks";
import Security from "@/components/sections/Security";
import OpenSource from "@/components/sections/OpenSource";
import Testimonials from "@/components/sections/Testimonials";
import Footer from "@/components/sections/Footer";

export default function Home() {
  return (
    <main className="min-h-screen bg-black text-white">
      <Hero />
      <Visualization />
      <PainPoint />
      <HowItWorks />
      <Security />
      <OpenSource />
      <Testimonials />
      <Footer />
    </main>
  );
}
