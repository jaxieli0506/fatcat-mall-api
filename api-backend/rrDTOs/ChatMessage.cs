namespace api_backend.rrDOTs
{
    public class ChatMessage
    {
        public string Type { get; set; } // "text", "image", "video" etc.
        public string Message { get; set; } // 內容 (文字或 URL)
    }
}