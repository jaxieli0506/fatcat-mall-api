//// 檔案路徑: /Services/LiveStreamService.cs
//using Amazon.IVS;
//using Amazon.IVS.Model;
//using api_backend.Models;
//using api_backend.Models.Enums;
//using Microsoft.EntityFrameworkCore; 
//using api_backend.Data; 

//namespace api_backend.Services;

//public class LiveStreamService
//{
//    private readonly YourDbContext _context; 
//    private readonly IAmazonIVS _ivsClient;

//    public LiveStreamService(YourDbContext context, IAmazonIVS ivsClient)
//    {
//        _context = context;
//        _ivsClient = ivsClient;
//    }

//    public async Task<Livestream> GetAndUpdateCurrentStreamAsync()
//    {
//        var liveStream = await _context.Livestreams
//            .FirstOrDefaultAsync(l => l.StreamStatus == (int)StreamStatusEnum.Live);

//        if (liveStream != null)
//        {
//            try
//            {
//                await _ivsClient.GetStreamAsync(new GetStreamRequest { ChannelArn = liveStream.IvsChannelArn });
//                return liveStream; 
//            }
//            catch (StreamNotFoundException)
//            {
//                liveStream.StreamStatus = (int)StreamStatusEnum.Ended;
//                liveStream.EndedAt = DateTime.UtcNow;
//                await _context.SaveChangesAsync();
//                return null; 
//            }
//        }
//        var nextScheduled = await _context.Livestreams
//            .Where(l => l.StreamStatus == (int)StreamStatusEnum.Scheduled)
//            .OrderBy(l => l.ScheduledAt)
//            .FirstOrDefaultAsync();

//        if (nextScheduled != null)
//        {
//            try
//            {
//                await _ivsClient.GetStreamAsync(new GetStreamRequest { ChannelArn = nextScheduled.IvsChannelArn });
//                
//                nextScheduled.StreamStatus = (int)StreamStatusEnum.Live;
//                nextScheduled.StartedAt = DateTime.UtcNow;
//                await _context.SaveChangesAsync();
//                return nextScheduled;
//            }
//            catch (StreamNotFoundException)
//            {
//                
//                return null;
//            }
//        }
//        return null; 
//    }
//}