FROM node:26-alpine AS build

WORKDIR /src
COPY package*.json /src/
RUN npm ci

# Copy the rest of the source code
COPY . /src
# Build the Angular app
ARG ANGULAR_ENVIRONMENT
RUN npx ng build --configuration="$ANGULAR_ENVIRONMENT"

# Stage 2: Serve the app with Nginx
FROM nginx:stable-alpine

# Copy the build output to replace the default Nginx contents
COPY --from=build /src/dist/tts-web/browser /usr/share/nginx/html

# Copy the Nginx configuration
COPY nginx/nginx.conf /etc/nginx/conf.d/default.conf

# Install Nano
RUN apk update && apk add nano
