# syntax=docker/dockerfile:latest@sha256:87999aa3d42bdc6bea60565083ee17e86d1f3339802f543c0d03998580f9cb89
FROM mcr.microsoft.com/devcontainers/base:3-noble@sha256:405175cbb232701dc27c47a2f178da555ea3ad20f85ea4142ad6a24f877af399

ARG GO_VERSION=1.26.3
ARG NODE_VERSION=24.16.0

RUN mkdir /usr/local/go && \
    curl -fqsLS https://go.dev/dl/go${GO_VERSION}.linux-amd64.tar.gz -o go.tar.gz && \
    tar -C /usr/local/go -xzf go.tar.gz --strip-components=1 && \
    rm go.tar.gz

RUN mkdir /usr/local/node && \
    curl -fqsLS https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.xz -o node.tar.xz && \
    tar -C /usr/local/node -xJf node.tar.xz --strip-components=1 && \
    rm node.tar.xz

ENV PATH=/usr/local/go/bin:/usr/local/node/bin:$PATH
ENV GOTOOLCHAIN=local

RUN sed -i.bak 's|http://archive.ubuntu.com/ubuntu/|https://ftp.udx.icscoe.jp/Linux/ubuntu/|g' /etc/apt/sources.list.d/ubuntu.sources

USER vscode

RUN mkdir -p /home/vscode/.nuget/packages && \
    mkdir -p /home/vscode/.claude

RUN cat >> /home/vscode/.bashrc << 'EOF'
alias claude='npx claude'
alias devcontainer='npx devcontainer'
EOF

WORKDIR /workspaces
