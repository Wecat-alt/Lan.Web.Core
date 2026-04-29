# Copilot Instructions

## 项目指南
- For NovaPlayer on Linux in this repo, the native library path is explicitly `NovaPlayer/lib/libNovaPlayer.so`; do not add an `x64` fallback for Linux unless requested.

## Technical Implementation Guidelines
- When implementing technical changes (like sharding), preserve existing business query logic exactly and avoid behavior changes.
- In this project, do not auto-create indexes for trackinfo monthly shard tables because base tables already have indexes managed separately.