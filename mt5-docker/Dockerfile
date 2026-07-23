FROM ghcr.io/linuxserver/baseimage-kasmvnc:ubuntujammy

# set version label
ARG BUILD_DATE
ARG VERSION
LABEL build_version="Metatrader5 Docker:- ${VERSION} Build-date:- ${BUILD_DATE}"
LABEL maintainer="github@gmartin.net"

# title
ENV TITLE=Metatrader5
ENV WINEPREFIX="/config/.wine"

# Update and upgrade
RUN apt-get update && apt-get upgrade -y

# Download wine using wget
RUN apt-get install wget && \
    wget -nc https://dl.winehq.org/wine-builds/ubuntu/dists/jammy/winehq-jammy.sources

# Move it source directory
RUN mv winehq-jammy.sources /etc/apt/sources.list.d/

# Download and add the repository key
RUN mkdir -pm755 /etc/apt/keyrings && \
    wget -O /etc/apt/keyrings/winehq-archive.key https://dl.winehq.org/wine-builds/winehq.key

# install wine and dependencies
RUN apt-get install -y --no-install-recommends winehq-staging

# install wine and dependencies
# RUN apt-get update && \
#     apt-get install -y --no-install-recommends wine winehq-staging:i386 libwine && \
#     apt-get install -y xdg-utils && \
#     apt-get install -y python3 python3-pip && \
#     pip3 install pyxdg && \
#     # Clean up to reduce image size
#     apt-get clean && \
#     rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# add local files
COPY /Metatrader /Metatrader
RUN chmod +x /Metatrader/start.sh
COPY /root /

# Remove sudo - It's not recommended to remove sudo unless absolutely necessary
# RUN apt-get purge -y sudo

# ports and volumes
EXPOSE 3000

VOLUME /config
    
