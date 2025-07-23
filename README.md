# 🧠 Refract

![License: MIT](https://img.shields.io/badge/license-MIT-green)
![Reverse Engineering](https://img.shields.io/badge/focus-Reverse%20Engineering-blue)

**Understand binaries. Ask questions. Get answers.**

Refract is an AI-powered reverse engineering assistant. It transforms binaries into high-level insights using disassembly, decompilation, and retrieval-augmented generation. Just ask your question — Refract finds the answer.

## 🔍 What It Does

- **Analyzes binaries**: Disassembles and decompiles code using static analysis.
- **Indexes knowledge**: Stores functions and metadata in a searchable format.
- **Answers questions**: Uses a language model to respond to natural language queries about program behavior, secrets, or structure.

Whether you're looking for a flag in a CTF binary or trying to understand obscure logic in a proprietary executable, Refract helps you get there faster.

## 🔰 Getting Started
### Installation
Download one of the nomic-embed-code GGUF files for embedding and add it to the models folder:
```
https://huggingface.co/nomic-ai/nomic-embed-code-GGUF/blob/main/README.md
```
Build the model, replace `[version]` with the downloaded version. We prepared a couple Modefile's, if yours is missing, add it:
```
docker exec -it embedder ollama create nomic-embed-code -f /root/.ollama/models/Modelfile-[version]
```
Also pull the mistral model for our LLM queries:
```
docker exec -it llm-server ollama pull mistral
```
Verify available models:
```
docker exec -it embedder ollama list
docker exec -it llm-server ollama list
```

### Execution
Run the backend:
```
docker compose up -d
```
Run the CLI:
```
docker compose run --rm refractcli dotnet run Refract.CLI 
```

## 🚀 Example

Based on the SpookyPass challenge: https://app.hackthebox.com/challenges/SpookyPass.

> **Q:** What is the secret value returned by the main function?  
> **A:** The main function doesn't directly return a secret value. However, if the password entered matches "s3cr3t_p455_f0r_gh05t5_4nd_gh0ul5", it prints out the hidden message "HTB{**********}" which can be considered as the secret value.

You can ask things like:

- What does this function do?
- Where is the flag stored?
- Which functions call `malloc`?
- Are there any obfuscated loops?

## 🛠️ Status

> 🔬 **Proof of concept:** No Touching! --George

## 🤝 Contributing

We welcome contributions from reverse engineers, CTF players, and tooling nerds. Open an issue, submit a PR, or suggest new use cases — every bit helps.

> 💡 Reverse engineering is tedious. Refract makes it conversational.