# syntax=docker/dockerfile:latest@sha256:87999aa3d42bdc6bea60565083ee17e86d1f3339802f543c0d03998580f9cb89
FROM mcr.microsoft.com/devcontainers/base:3-resolute@sha256:bb779775cfc7d2e65f9b0f80f17770878f5a50c00d15cc873b627c5c214ce37e

RUN sed -i.bak 's|http://archive.ubuntu.com/ubuntu/|https://ftp.udx.icscoe.jp/Linux/ubuntu/|g' /etc/apt/sources.list.d/ubuntu.sources

USER vscode

RUN mkdir -p /home/vscode/.nuget/packages && \
    mkdir -p /home/vscode/.claude

RUN cat >> /home/vscode/.bashrc << 'EOF'
alias claude='npx claude'
alias devcontainer='npx devcontainer'
EOF

WORKDIR /workspaces
