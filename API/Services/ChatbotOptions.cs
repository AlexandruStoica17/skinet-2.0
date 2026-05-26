namespace API.Services
{
    public class ChatbotOptions
    {
        public string ApiKey { get; set; }
        public string Model { get; set; } = "gpt-5-mini";
        public string Endpoint { get; set; } = "https://api.openai.com/v1/responses";
        public string KnowledgePath { get; set; } = "Content/ChatbotKnowledge";
        public int MaxKnowledgeChunks { get; set; } = 4;
        public int MaxHistoryMessages { get; set; } = 8;
        public int MaxOutputTokens { get; set; } = 650;
    }
}
