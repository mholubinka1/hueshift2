FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app
COPY HueShift2/*.sln ./
COPY HueShift2/HueShift2/*.csproj ./HueShift2/
COPY HueShift2/HueShift2Tests/*.csproj ./HueShift2Tests/
RUN dotnet restore

COPY HueShift2 ./
RUN dotnet build
FROM build-env AS test-runner
WORKDIR /app/HueShift2Tests/

FROM build-env AS unit-test
LABEL unit-test=true
WORKDIR /app/HueShift2Tests/
RUN dotnet test --results-directory ./ --logger:"junit;LogFileName=unit_test_report.xml" /p:CollectCoverage=true /p:CoverletOutput='coverage.xml' /p:CoverletOutputFormat=opencover

FROM build-env AS publish
WORKDIR /app/HueShift2/
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS runtime
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
