FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY HueShift2/*.csproj ./

RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime
WORKDIR /app

RUN mkdir -p config
VOLUME /config

RUN mkdir -p log
VOLUME /log

ENV UDPPORT 6454

EXPOSE ${UDPPORT}
EXPOSE ${UDPPORT}/udp

COPY --from=build /app/HueShift/out ./

ENTRYPOINT ["dotnet", "HueShift.dll", "--configuration-file", "/config/hueshift2-config.json"]