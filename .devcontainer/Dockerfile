# syntax=docker/dockerfile:latest@sha256:87999aa3d42bdc6bea60565083ee17e86d1f3339802f543c0d03998580f9cb89
FROM mcr.microsoft.com/devcontainers/base:3-noble@sha256:405175cbb232701dc27c47a2f178da555ea3ad20f85ea4142ad6a24f877af399

RUN sed -i.bak 's|http://archive.ubuntu.com/ubuntu/|https://ftp.udx.icscoe.jp/Linux/ubuntu/|g' /etc/apt/sources.list.d/ubuntu.sources

USER vscode

RUN mkdir -p /home/vscode/.nuget/packages && \
    mkdir -p /home/vscode/.claude

RUN cat >> /home/vscode/.bashrc << 'EOF'
alias claude='npx claude'
alias devcontainer='npx devcontainer'
EOF

WORKDIR /workspaces
