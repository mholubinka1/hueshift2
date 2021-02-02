FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY HueShift2/HueShift2/*.csproj ./HueShift2/

WORKDIR /app/HueShift2
RUN dotnet restore

COPY HueShift2/HueShift2/. ./HueShift2/
WORKDIR /app/HueShift2
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime
WORKDIR /app/HueShift2

RUN mkdir -p config
VOLUME /config

RUN mkdir -p log
VOLUME /log

ENV UDPPORT 6454
EXPOSE ${UDPPORT}
EXPOSE ${UDPPORT}/udp

COPY --from=build-env /app/out ./

ENTRYPOINT ["dotnet", "HueShift2.dll", "--configuration-file", "/config/hueshift2-config.json"]