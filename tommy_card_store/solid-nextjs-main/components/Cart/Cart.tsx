"use client";
import { motion } from "framer-motion";
import Image from "next/image";
import Link from "next/link";
import { useState } from "react";

const items = [
  {
    id: 1,
    name: "Example Item 1",
    price: 49.99,
    image: "/images/blog/basketball/1.jpg",
    pickupLocation: "Store Name 1",
  },
  {
    id: 2,
    name: "Example Item 2",
    price: 59.99,
    image: "/images/blog/basketball/1.jpg",
    pickupLocation: "Store Name 2",
  },
  {
    id: 3,
    name: "Example Item 3",
    price: 59.99,
    image: "/images/blog/basketball/1.jpg",
    pickupLocation: "Store Name 3",
  },
  // Add more items as needed
];

const Cart = () => {
  // const [data, setData] = useState({
  //   email: "",
  //   password: "",
  // });

  return (
    <>
      {/* <!-- ===== Order Form Start ===== --> */}
      <section className="pb-12.5 pt-32.5 lg:pb-25 lg:pt-45 xl:pb-30 xl:pt-50">
        <div className="relative z-1 mx-auto max-w-c-1216 px-7.5 pb-7.5 pt-10 lg:px-15 lg:pt-15 xl:px-20 xl:pt-20 flex">
          <div className="absolute left-0 top-0 -z-1 h-2/3 w-[140%] -ml-[20%] rounded-lg bg-gradient-to-t from-transparent to-[#dee7ff47] dark:bg-gradient-to-t dark:to-[#252A42]"></div>
          {/* <div className="absolute bottom-17.5 left-0 -z-1 h-1/3 w-full">
            <Image
              src="/images/shape/shape-dotted-light.svg"
              alt="Dotted"
              className="dark:hidden"
              fill
            />
            <Image
              src="/images/shape/shape-dotted-dark.svg"
              alt="Dotted"
              className="hidden dark:block"
              fill
            />
          </div> */}

          <motion.div
            variants={{
              hidden: {
                opacity: 0,
                y: -20,
              },

              visible: {
                opacity: 1,
                y: 0,
              },
            }}
            initial="hidden"
            whileInView="visible"
            transition={{ duration: 1, delay: 0.1 }}
            viewport={{ once: true }}
            className="animate_top rounded-lg bg-white px-7.5 pt-7.5 shadow-solid-8 dark:border dark:border-strokedark dark:bg-black xl:px-15 xl:pt-15"
          >
            <h2 className="mb-15 text-center text-3xl font-semibold text-black dark:text-white xl:text-sectiontitle2">
              Cart
            </h2>
            <div className="flex flex-col">
              <div className="flex items-center gap-8">

              </div>
            </div>

            <form>
              <div className="mb-7.5 flex flex-col gap-7.5 lg:mb-12.5 lg:flex-row lg:justify-between lg:gap-14">
                {/* <input
                  type="text"
                  placeholder="Email"
                  name="email"
                  value={data.email}
                  onChange={(e) => setData({ ...data, email: e.target.value })}
                  className="w-full border-b border-stroke !bg-white pb-3.5 focus:border-waterloo focus:placeholder:text-black focus-visible:outline-none dark:border-strokedark dark:!bg-black dark:focus:border-manatee dark:focus:placeholder:text-white lg:w-1/2"
                />

                <input
                  type="password"
                  placeholder="Password"
                  name="password"
                  value={data.password}
                  onChange={(e) =>
                    setData({ ...data, password: e.target.value })
                  }
                  className="w-full border-b border-stroke !bg-white pb-3.5 focus:border-waterloo focus:placeholder:text-black focus-visible:outline-none dark:border-strokedark dark:!bg-black dark:focus:border-manatee dark:focus:placeholder:text-white lg:w-1/2"
                /> */}
              </div>

              <div className="flex flex-wrap items-center gap-10 md:justify-between xl:gap-15">
                <div className="flex flex-wrap gap-4 md:gap-10">
                  <div className="mb-4 items-center">
                    {/* <input
                      id="default-checkbox"
                      type="checkbox"
                      className="peer sr-only"
                    /> */}
                    {items.map((item) => (
          <div key={item.id} className="bg-white rounded-lg shadow-lg p-5 mb-5 flex">
            <img src={item.image} alt="Product Image" className="w-24 h-32 object-cover rounded-lg mr-5" />
            <div>
              <h3 className="text-xl font-semibold mb-5">{item.name}</h3>
              <div className="flex justify-between items-center mb-4">
                <span>Price: ${item.price.toFixed(2)}</span>
                <div className="flex items-center">
                  <label htmlFor={`quantity-${item.id}`} className="mr-2">Quantity:</label>
                  <select id={`quantity-${item.id}`} name="quantity" className="border rounded p-1">
                    {[...Array(10).keys()].map((i) => (
                      <option key={i + 1} value={i + 1}>{i + 1}</option>
                    ))}
                  </select>
                  <button
                    aria-label="remove item"
                    className="inline-flex items-center gap-2.5 rounded-full bg-black px-4 py-1 font-medium text-white duration-300 ease-in-out hover:bg-blackho dark:bg-btndark dark:hover:bg-blackho ml-2"
                  >
                    Remove
                  </button>
                </div>
              </div>
              <div className="flex items-center">
                <span className="mr-2">Pickup at:</span>
                <span className="bg-gray-200 text-gray-700 rounded-full px-3 py-1">{item.pickupLocation}</span>
              </div>
            </div>
          </div>
        ))}

                    {/* <span className="border-gray-300 bg-gray-100 text-blue-600 dark:border-gray-600 dark:bg-gray-700 group mt-1 flex h-5 min-w-[20px] items-center justify-center rounded peer-checked:bg-primary">
                      <svg
                        className="opacity-0 peer-checked:group-[]:opacity-100"
                        width="10"
                        height="8"
                        viewBox="0 0 10 8"
                        fill="none"
                        xmlns="http://www.w3.org/2000/svg"
                      >
                        <path
                          fillRule="evenodd"
                          clipRule="evenodd"
                          d="M9.70704 0.792787C9.89451 0.980314 9.99983 1.23462 9.99983 1.49979C9.99983 1.76495 9.89451 2.01926 9.70704 2.20679L4.70704 7.20679C4.51951 7.39426 4.26521 7.49957 4.00004 7.49957C3.73488 7.49957 3.48057 7.39426 3.29304 7.20679L0.293041 4.20679C0.110883 4.01818 0.0100885 3.76558 0.0123669 3.50339C0.0146453 3.24119 0.119814 2.99038 0.305222 2.80497C0.490631 2.61956 0.741443 2.51439 1.00364 2.51211C1.26584 2.50983 1.51844 2.61063 1.70704 2.79279L4.00004 5.08579L8.29304 0.792787C8.48057 0.605316 8.73488 0.5 9.00004 0.5C9.26521 0.5 9.51951 0.605316 9.70704 0.792787Z"
                          fill="white"
                        />
                      </svg>
                    </span> */}
                    {/* <label
                      htmlFor="default-checkbox"
                      className="flex max-w-[425px] cursor-pointer select-none pl-3"
                    >
                      Keep me signed in
                    </label> */}
                  </div>

                  {/* <a href="#" className="hover:text-primary">
                    Forgot Password?
                  </a> */}
                </div>

                {/* <button
                  aria-label="login with email and password"
                  className="inline-flex items-center gap-2.5 rounded-full bg-black px-6 py-3 font-medium text-white duration-300 ease-in-out hover:bg-blackho dark:bg-btndark dark:hover:bg-blackho"
                >
                  Log in
                  <svg
                    className="fill-white"
                    width="14"
                    height="14"
                    viewBox="0 0 14 14"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M10.4767 6.16664L6.00668 1.69664L7.18501 0.518311L13.6667 6.99998L7.18501 13.4816L6.00668 12.3033L10.4767 7.83331H0.333344V6.16664H10.4767Z"
                      fill=""
                    />
                  </svg>
                </button> */}
              </div>

              {/* <div className="mt-12.5 border-t border-stroke py-5 text-center dark:border-strokedark">
                <p>
                  Don't have an account?{" "}
                  <Link
                    className="text-black hover:text-primary dark:text-white hover:dark:text-primary"
                    href="/auth/signup"
                  >
                    Sign Up
                  </Link>
                </p>
              </div> */}
            </form>
          </motion.div>
          <div className="flex-1 lg:ml-10 mt-10 lg:mt-0">
    <div className="bg-white rounded-lg shadow-lg p-5">
      <h3 className="text-xl font-semibold mb-5">Order Summary</h3>
      <div className="mb-5">
      <div className="flex justify-between mb-2">
        <span>Original Price</span>
        <span>$99.99</span>
      </div>
      <div className="flex justify-between mb-2">
        <span>Savings</span>
        <span>-$20.00</span>
      </div>
      <div className="flex justify-between mb-2">
        <span>Store Pickup</span>
        <span>FREE</span>
      </div>
      <div className="flex justify-between mb-2">
        <span>Estimated Sales Tax</span>
        <span>Calculated in checkout</span>
      </div>
      <div className="flex justify-between font-semibold">
        <span>Total</span>
        <span>$79.99</span>
      </div>
    </div>
      {
        <button
        className="inline-flex w-full text-center items-center justify-center gap-2.5 rounded-full bg-black px-6 py-3 font-medium text-white duration-300 ease-in-out hover:bg-blackho dark:bg-btndark dark:hover:bg-blackho"
      >
        Checkout
        {/* <svg
          className="fill-white"
          width="14"
          height="14"
          viewBox="0 0 14 14"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M10.4767 6.16664L6.00668 1.69664L7.18501 0.518311L13.6667 6.99998L7.18501 13.4816L6.00668 12.3033L10.4767 7.83331H0.333344V6.16664H10.4767Z"
            fill=""
          />
        </svg> */}
      </button>
      }
    </div>
  </div>
        </div>

      </section>
      {/* <!-- ===== SignIn Form End ===== --> */}
    </>
  );
};

export default Cart;
