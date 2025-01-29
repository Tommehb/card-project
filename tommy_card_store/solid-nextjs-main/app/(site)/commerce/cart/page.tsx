import Cart from "@/components/Cart/Cart";
import { Metadata } from "next";

export const metadata: Metadata = {
  title: "Login Page - Solid SaaS Boilerplate",
  description: "This is Login page for Startup Pro",
  // other metadata
};

const CartPage = () => {
  return (
    <>
      <Cart />
    </>
  );
};

export default CartPage;
