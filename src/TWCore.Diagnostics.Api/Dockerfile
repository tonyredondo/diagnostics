﻿FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine3.9
COPY ./app/. ./App/.

ENV TWCORE_FORCE_ENVIRONMENT=Docker
ENV DIAGNOSTICS_QUEUE_ROUTE=amqp://test:test@127.0.0.1:5672/
ENV DIAGNOSTICS_QUEUE_NAME=DIAGNOSTICS_RQ
ENV DIAGNOSTICS_STATUS_HTTP_PORT=28905
ENV DIAGNOSTICS_POSTGRES_CONNECTIONSTRING=Host=93.189.37.106;Port=5432;User ID=diaguser;Password=diagpassword;Database=diagnostics;Pooling=true;
ENV DIAGNOSTICS_BOT_DEFAULTENVIRONMENT=Docker
ENV DIAGNOSTICS_BOT_SLACKTOKEN=Token
ENV DIAGNOSTICS_MESSAGING_ENABLED=false
ENV DIAGNOSTICS_QUEUE_CONFIG=./settings/queues.redis.xml

EXPOSE 55999/tcp
EXPOSE 28905/tcp

VOLUME /App/logs
VOLUME /App/settings
VOLUME /App/assemblies
VOLUME /App/tracesdata

WORKDIR /App
ENTRYPOINT [ "./TWCore.Diagnostics.Api" ]