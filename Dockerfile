# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY --link . .
RUN dotnet restore -a $TARGETARCH
RUN dotnet publish ./src/BrickDex.Web/BrickDex.Web.csproj -a $TARGETARCH --no-restore -o /app

# Build frontend
FROM node:24-alpine AS frontend
WORKDIR /source

COPY --link ./src/BrickDex.Frontend .
RUN yarn
RUN yarn build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN mkdir -p lucene
RUN mkdir -p wwwroot

RUN chown -R app /app/lucene

COPY --link --from=build /app .
COPY --link --from=frontend /source/artifacts ./wwwroot

USER $APP_UID

EXPOSE 8080/tcp

ENTRYPOINT ["./BrickDex.Web"]
