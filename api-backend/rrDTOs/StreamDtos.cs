namespace api_backend.rrDTOs
{
    // DTOs/AnalysisDtos.cs (你可以在現有檔案加入，或建立新檔)

    // 用於 /start 回傳給 OBS 的資訊
    public class StreamCredentialsDto
    {
        public string IngestEndpoint { get; set; }
        public string StreamKey { get; set; }
    }

    // 用於 /current 回傳給觀眾的資訊
    public class CurrentStreamDto
    {
        public string Title { get; set; }
        public string PlaybackUrl { get; set; }

       public string ChatRoomArn { get; set; } // 讓前端知道要加入哪個聊天室
    }

    // 用於 /start 接收的參數
    public class StartStreamRequest
    {
        public string Title { get; set; }
    }


}
