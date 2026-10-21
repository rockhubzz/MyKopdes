FROM node:20-alpine AS base
WORKDIR /app

ARG NEXT_PUBLIC_API_BASE_URL=/api
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL

COPY package.json package-lock.json* ./
RUN npm install

# ---- development target ---------------------------------------------------
FROM base AS development
COPY . .
EXPOSE 3000
CMD ["npm", "run", "dev"]

# ---- build stage for production -------------------------------------------
FROM base AS build
COPY . .
# Next.js works without a public/ dir, but the production stage copies it
# explicitly — make sure it exists even on the freshest checkout.
RUN mkdir -p /app/public
RUN npm run build

# ---- production target -----------------------------------------------------
FROM node:20-alpine AS production
WORKDIR /app
ENV NODE_ENV=production
# Docker sets HOSTNAME=<container-hostname> in the environment, and Next's
# standalone server binds to exactly that — leaving out 0.0.0.0 breaks the
# localhost healthcheck and any mapping other than the container IP.
ENV HOSTNAME=0.0.0.0
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public
EXPOSE 3000
CMD ["node", "server.js"]
