FROM node:24-alpine AS build
WORKDIR /app

COPY client/sms-angular/package.json client/sms-angular/package-lock.json ./
RUN npm ci
COPY client/sms-angular/ ./
RUN npm run build

FROM nginx:1.29-alpine AS final
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/sms-angular/browser /usr/share/nginx/html
EXPOSE 80
