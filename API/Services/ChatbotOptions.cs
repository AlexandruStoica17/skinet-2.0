namespace API.Services
{
    public class ChatbotOptions
    {
        public string Provider { get; set; } = "Ollama";
        public string ApiKey { get; set; }
        public string Model { get; set; } = "gemma3:4b";
        public string Endpoint { get; set; } = "https://api.openai.com/v1/responses";
        public string OllamaEndpoint { get; set; } = "http://localhost:11434/api/chat";
        public string KnowledgePath { get; set; } = "Content/ChatbotKnowledge";
        public int MaxKnowledgeChunks { get; set; } = 4;
        public int MaxHistoryMessages { get; set; } = 8;
        public int MaxOutputTokens { get; set; } = 650;
        public double Temperature { get; set; } = 0.2;
    }
}
