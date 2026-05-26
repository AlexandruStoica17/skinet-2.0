namespace API.Dtos
{
    public class ChatbotResponseDto
    {
        public string Answer { get; set; }
        public IReadOnlyList<string> Sources { get; set; } = Array.Empty<string>();
        public string Mode { get; set; }
        public bool IsAiConfigured { get; set; }
    }
}
