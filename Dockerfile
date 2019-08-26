FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS base
RUN mkdir App
WORKDIR /src
COPY . /src
RUN dotnet restore -r linux-x64
RUN dotnet build -c Release -r linux-x64
RUN dotnet publish -c Release -r linux-x64 -v q -o /App/

FROM mcr.microsoft.com/dotnet/core/runtime:2.2
WORKDIR /App
COPY --from=base /App ./

ENV DIAGNOSTICS_MESSAGING_ENABLED="false"
ENV TWCORE_FORCE_ENVIRONMENT="Docker"
ENV DIAGNOSTICS_QUEUE_ROUTE="amqp://test:test@127.0.0.1:5672/"
ENV DIAGNOSTICS_QUEUE_NAME="DIAGNOSTICS_RQ"
ENV DIAGNOSTICS_STATUS_HTTP_PORT="28905"
ENV DIAGNOSTICS_POSTGRES_CONNECTIONSTRING=""
ENV DIAGNOSTICS_BOT_DEFAULTENVIRONMENT="Docker"
ENV DIAGNOSTICS_BOT_SLACKTOKEN="Token"
ENV DIAGNOSTICS_QUEUE_CONFIG="./settings/queues.redis.xml"
ENV DIAGNOSTICS_USE_NEW_PATH_STRUCTURE="false"

EXPOSE 55999/tcp
EXPOSE 28905/tcp

VOLUME /App/logs
VOLUME /App/settings
VOLUME /App/assemblies
VOLUME /App/tracesdata

WORKDIR /App
ENTRYPOINT [ "./TWCore.Diagnostics.Api" ]