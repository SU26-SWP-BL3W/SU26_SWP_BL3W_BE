import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  async redirects() {
    return [
      {
        source: '/register',
        destination: '/auth?mode=register',
        permanent: false,
      },
      {
        source: '/login',
        destination: '/auth?mode=login',
        permanent: false,
      },
    ];
  },
};

export default nextConfig;