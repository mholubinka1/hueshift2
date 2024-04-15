FROM mcr.microsoft.com/dotnet/sdk:7.0.407-bookworm-slim-arm64v8 AS build-env
WORKDIR /app
COPY HueShift2/*.sln ./
COPY HueShift2/HueShift2/*.csproj ./HueShift2/
RUN dotnet restore
COPY HueShift2 ./
RUN dotnet build

FROM build-env AS publish
WORKDIR /app/HueShift2/
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:7.0.407-bookworm-slim-arm64v8 AS runtime
WORKDIR /app
RUN mkdir -p config
VOLUME /config
RUN mkdir -p log
VOLUME /log
ENV UDPPORT 6454
EXPOSE ${UDPPORT}
EXPOSE ${UDPPORT}/udp
COPY --from=publish /app/HueShift2/out ./
ENTRYPOINT ["dotnet", "HueShift2.dll", "--config-file", "/config/hueshift2-config.json"]
