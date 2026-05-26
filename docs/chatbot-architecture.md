# GreenBeauty Chatbot Architecture

## Purpose

The GreenBeauty chatbot is designed as a professional support assistant for marketplace users. It answers common questions about orders, products, sellers, ingredients, reviews, favorites and platform navigation.

## Architecture

The implementation uses a lightweight Retrieval-Augmented Generation pattern:

1. The API receives a user question through `POST /api/chatbot/ask`.
2. The chatbot service loads curated Markdown documents from `API/Content/ChatbotKnowledge`.
3. A lexical retriever scores knowledge chunks against the user question.
4. Only the most relevant chunks are sent to the language model.
5. The model generates a concise answer grounded in the retrieved context.
6. If OpenAI is not configured or the request fails, the API returns a local retrieval fallback instead of failing the UI.

## Configuration

The API reads chatbot settings from the `OpenAI` configuration section:

- `ApiKey`: secret API key, configured with user secrets or environment variables.
- `Model`: default model, currently `gpt-5-mini`.
- `Endpoint`: OpenAI Responses API endpoint.
- `KnowledgePath`: relative path to the Markdown knowledge base.
- `MaxKnowledgeChunks`: maximum number of retrieved chunks sent to the model.
- `MaxHistoryMessages`: maximum conversation history messages used for context.
- `MaxOutputTokens`: response size limit.

Recommended local setup:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY" --project API
```

For deployment, use an environment variable:

```text
OpenAI__ApiKey=YOUR_OPENAI_API_KEY
```

## Safety

The assistant is constrained by a system instruction that prevents medical diagnosis, treatment claims, payment-data requests, password requests and private order-data disclosure. Order-specific answers should direct users to authenticated platform pages such as My Orders.

## Optimization

The chatbot avoids sending the full application context to the model. It sends only the relevant knowledge snippets and the last few conversation messages. This reduces cost, improves latency and lowers hallucination risk.
