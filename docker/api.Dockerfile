# Specify ARG at the beginning to allow it to be passed at build time
ARG API_NAME

# Stage 1: Build and publish the ASP.NET Core app
FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine AS build
WORKDIR /src

# Re-declare ARG here because ARGs are not available across stages
ARG API_NAME

COPY . .
RUN dotnet restore "${API_NAME}" 
RUN dotnet publish "${API_NAME}/${API_NAME}.csproj" --no-restore -c Release -o /app/publish

# Stage 2: Create final image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

HEALTHCHECK --interval=5s --timeout=10s --retries=3 CMD curl --silent --fail http://localhost/health || exit 1

# Install apps for debugging purposes
RUN apk update && apk add nano && apk add curl

WORKDIR /app

# Re-declare ARG and set it as an ENV variable for use at runtime
ARG API_NAME
ENV API_NAME=$API_NAME
COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet ${API_NAME}.dll"]