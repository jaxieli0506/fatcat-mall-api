// 檔案路徑: /Models/Enums/StreamStatusEnum.cs
namespace api_backend.Models.Enums; 

public enum StreamStatusEnum
{
    Scheduled = 0, //0: 排程中 (Scheduled) - 直播已建立在資料庫，但尚未開始推流。
    Live = 1,  //1: 直播中 (Live) - OBS 正在推流，觀眾可以觀看。
    Ended = 2  //2: 已結束 (Ended) - 直播已結束。
}