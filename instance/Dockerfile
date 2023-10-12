FROM mcr.microsoft.com/dotnet/sdk:8.0.100-rc.2-bookworm-slim-amd64 AS publish
WORKDIR /src
COPY . .
RUN dotnet restore "Instance/Instance.csproj" -r linux-x64

RUN dotnet publish "Instance/Instance.csproj" \
    -c Release \
    --no-restore \
    -o /publish \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0.0-rc.2-bookworm-slim-amd64 as runtime
WORKDIR /app
COPY --from=publish /publish .
RUN chmod +x *

ENV ASPNETCORE_URLS=http://+:11111
CMD ["./Instance"]

EXPOSE 27015/tcp 
EXPOSE 27015/udp 
EXPOSE 11111/tcp
