# GreenBeauty Chatbot Architecture

## Purpose

The GreenBeauty chatbot is designed as a professional support assistant for marketplace users. It answers common questions about orders, products, sellers, ingredients, reviews, favorites and platform navigation.

## Architecture

The implementation uses a local Retrieval-Augmented Generation pattern:

1. The API receives a user question through `POST /api/chatbot/ask`.
2. The chatbot service loads curated Markdown documents from `API/Content/ChatbotKnowledge`.
3. The service also searches the live product catalog from SQLite.
4. A lexical retriever scores knowledge chunks and products against the user question.
5. Only the most relevant snippets are sent to the language model.
6. Ollama runs an open-source model locally and generates a grounded answer.
7. If Ollama is not running, the API returns a deterministic local retrieval fallback instead of failing the UI.

This keeps the feature free to run locally and avoids sending marketplace data to a paid external API.

## Free Local AI Setup

Install Ollama on Windows, then pull the default model:

```powershell
ollama pull gemma3:4b
```

Recommended fallback for weaker hardware:

```powershell
ollama pull llama3.2:3b
```

If you switch model, update `Chatbot:Model` in `API/appsettings.Development.json`.

## Configuration

The API reads chatbot settings from the `Chatbot` configuration section:

- `Provider`: default provider, currently `Ollama`.
- `Model`: default local model, currently `gemma3:4b`.
- `OllamaEndpoint`: local Ollama chat endpoint, usually `http://localhost:11434/api/chat`.
- `KnowledgePath`: relative path to the Markdown knowledge base.
- `MaxKnowledgeChunks`: maximum number of retrieved chunks sent to the model.
- `MaxHistoryMessages`: maximum conversation history messages used for context.
- `MaxOutputTokens`: local model response size limit.
- `Temperature`: generation randomness; low values keep support answers stable.

OpenAI remains possible as an optional provider by setting `Chatbot:Provider` to `OpenAI` and configuring `Chatbot:ApiKey`, but it is not required.

## Safety

The assistant is constrained by a system instruction that prevents medical diagnosis, treatment claims, payment-data requests, password requests and private order-data disclosure. Order-specific answers should direct users to authenticated platform pages such as My Orders.

## Optimization

The chatbot avoids sending the full application context to the model. It sends only relevant knowledge snippets, relevant product matches and the last few conversation messages. This reduces latency and lowers hallucination risk even with a local model.
