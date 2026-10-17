FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG APP_BASE_HREF=/meals/
RUN npm run build -- --base-href="${APP_BASE_HREF}"

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/meal-book/browser /usr/share/nginx/html
EXPOSE 8080
