# syntax=docker/dockerfile:latest@sha256:87999aa3d42bdc6bea60565083ee17e86d1f3339802f543c0d03998580f9cb89
FROM mcr.microsoft.com/devcontainers/base:3-noble@sha256:405175cbb232701dc27c47a2f178da555ea3ad20f85ea4142ad6a24f877af399

RUN sed -i.bak 's|http://archive.ubuntu.com/ubuntu/|https://ftp.udx.icscoe.jp/Linux/ubuntu/|g' /etc/apt/sources.list.d/ubuntu.sources

# datasource=github-releases のものはバージョン番号に v をつける

# renovate: datasource=node-version depName=node
ARG NODE_VERSION=24.16.0

# renovate: datasource=npm depName=npm
ARG NPM_VERSION=11.16.0

# renovate: datasource=github-releases depName=bluesky-social/goat
ARG GOAT_VERSION=v0.2.3

# renovate: datasource=npm depName=@anthropic-ai/claude-code
ARG CLAUDE_CODE_VERSION=2.1.158

# renovate: datasource=npm depName=@devcontainers/cli
ARG DEVCONTAINER_VERSION=0.87.0

# renovate: datasource=npm depName=renovate
ARG RENOVATE_VERSION=43.205.2

# renovate: datasource=github-releases depName=microsoft/apm
ARG APM_VERSION=v0.16.1

RUN mkdir /usr/local/node && \
    curl -fqsLS https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.xz -o node.tar.xz && \
    tar -C /usr/local/node -xJf node.tar.xz --strip-components=1 && \
    rm node.tar.xz

RUN mkdir /usr/local/goat && \
    curl -fqsLS https://github.com/bluesky-social/goat/releases/download/${GOAT_VERSION}/goat_Linux_x86_64.tar.gz -o goat.tar.gz && \
    tar -C /usr/local/goat -xzf goat.tar.gz && \
    rm goat.tar.gz

RUN mkdir /usr/local/apm && \
    curl -fqsLS https://github.com/microsoft/apm/releases/download/${APM_VERSION}/apm-linux-x86_64.tar.gz -o apm.tar.gz && \
    tar -C /usr/local/apm -xzf apm.tar.gz --strip-components=1 && \
    rm apm.tar.gz

USER vscode

WORKDIR /home/vscode

RUN mkdir -p .nuget/packages .npm .npm-global .claude

ENV PATH=/usr/local/node/bin:/usr/local/goat:/usr/local/apm:/home/vscode/.npm-global/bin:$PATH

# << と 'EOF' の間に空白を開けない
# https://github.com/rcjsuen/dockerfile-utils/issues/130
RUN cat >> /home/vscode/.npmrc <<'EOF'
cache=/home/vscode/.npm
prefix=/home/vscode/.npm-global
save-exact=true
EOF

RUN npm install -g npm@$NPM_VERSION

RUN npm install -g \
    @anthropic-ai/claude-code@$CLAUDE_CODE_VERSION \
    @devcontainers/cli@$DEVCONTAINER_VERSION \
    renovate@$RENOVATE_VERSION

WORKDIR /workspaces
