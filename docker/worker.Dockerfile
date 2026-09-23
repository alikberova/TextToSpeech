FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore TextToSpeech.Worker --locked-mode
RUN dotnet publish TextToSpeech.Worker --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
RUN apk add --no-cache krb5-libs
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TextToSpeech.Worker.dll"]
