import { Menu } from "@/types/menu";

const menuData: Menu[] = [
  {
    id: 1,
    title: "Singles",
    newTab: false,
    path: "/",
  },
  {
    id: 2,
    title: "Mystery Boxes",
    newTab: false,
    path: "/#features",
  },
  {
    id: 2.1,
    title: "Search",
    newTab: false,
    path: "/blog",
  },
  {
    id: 2.3,
    title: "Account",
    newTab: false,
    path: "/docs",
  },
  // {
  //   id: 2.3,
  //   title: "Cart",
  //   newTab: false,
  //   path: "/docs",
  // },
  {
    id: 3,
    title: "Categories",
    newTab: false,
    submenu: [
      {
        id: 31,
        title: "Basketball",
        newTab: false,
        path: "/blog",
      },
      {
        id: 34,
        title: "Baseball",
        newTab: false,
        path: "/auth/signin",
      },
      {
        id: 35,
        title: "Football",
        newTab: false,
        path: "/auth/signup",
      },
      {
        id: 35,
        title: "Golf",
        newTab: false,
        path: "/auth/signup",
      },
      {
        id: 35,
        title: "Tennis",
        newTab: false,
        path: "/auth/signup",
      },
      {
        id: 35,
        title: "School Cards",
        newTab: false,
        path: "/auth/signup",
      },
      {
        id: 35,
        title: "Food Cards",
        newTab: false,
        path: "/docs",
      },
      {
        id: 35.1,
        title: "Pokemon Cards",
        newTab: false,
        path: "/support",
      },
      {
        id: 36,
        title: "Yu-Gi-Oh Cards",
        newTab: false,
        path: "/error",
      },
    ],
  },

  // {
  //   id: 4,
  //   title: "Support",
  //   newTab: false,
  //   path: "/support",
  // },
];

export default menuData;
