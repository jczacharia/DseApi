ARG baseimage
FROM $baseimage

USER root

# Higher-level port mapped to port 80/443 in OpenShift
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Flush stdout/stderr line-by-line so startup crashes actually surface in `oc logs`.
ENV DOTNET_CONSOLE_DISABLE_BUFFERING=1
ENV DOTNET_RUNNING_IN_CONTAINER=true

WORKDIR /app
ENV PATH=/app:${PATH} HOME=/app

COPY uid_entrypoint .
COPY app .

RUN chmod -R u+x /app && \
    chgrp -R 0 /app && \
    chmod -R g=u /app /etc/passwd

USER 10001

# uid_entrypoint resolves the (random) OpenShift UID into /etc/passwd, then exec's
# the dotnet process. Single ENTRYPOINT — having two silently drops the first.
ENTRYPOINT ["uid_entrypoint", "dotnet", "Dse.Api.dll"]
