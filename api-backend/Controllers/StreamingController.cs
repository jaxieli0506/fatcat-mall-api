using Amazon;
using Amazon.Runtime;
using Amazon.Ivschat;
using Amazon.Ivschat.Model;
using api_backend.DTOs;
using api_backend.Models;
using api_backend.rrDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreamingController : ControllerBase
    {
        private readonly TeamDb2Context _context;
        private readonly IConfiguration _configuration;

        public StreamingController(TeamDb2Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ========================================================================
        // API 端點 (API Endpoints)
        // ========================================================================

        // GET: /api/streaming/history - 取得過去的直播紀錄 
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<Livestream>>> GetStreamHistory()
        {
            var history = await _context.Livestreams
                .Where(l => l.StreamStatus == 2)
                .OrderByDescending(l => l.StartedAt)
                .Take(20)
                .ToListAsync();
            return Ok(history);
        }

        // POST: /api/streaming/start - 管理員開始一場新的直播 
        [HttpPost("start")]
        public async Task<ActionResult<StreamCredentialsDto>> StartStream([FromBody] StartStreamRequest request)
        {
            var existingLiveStream = await _context.Livestreams.FirstOrDefaultAsync(l => l.StreamStatus == 1);
            if (existingLiveStream != null) return BadRequest("已經有一場直播正在進行中。");

            var ivsConfig = _configuration.GetSection("IVS");
            var newStream = new Livestream            
            {
                Title = request.Title,
                StreamStatus = 1,
                StartedAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                ScheduledAt = DateTime.Now,
                AdminId = 1,
                IvsPlaybackUrl = ivsConfig["PlaybackUrl"],
                IvsStreamKey = ivsConfig["StreamKey"],
            };
            _context.Livestreams.Add(newStream);
            await _context.SaveChangesAsync();

            // 【★ 核心邏輯 ★】
            // Start API 的職責很單純：建立直播紀錄，並回傳 OBS 需要的 ingestEndpoint 和 streamKey。
            return Ok(new StreamCredentialsDto
            {
                IngestEndpoint = $"rtmps://{ivsConfig["IngestEndpoint"]}:443/app/",
                StreamKey = ivsConfig["StreamKey"]
            });
        }

        // POST: /api/streaming/end - 管理員結束當前的直播
        [HttpPost("end")]
        public async Task<IActionResult> EndStream()
        {
            var liveStream = await _context.Livestreams.FirstOrDefaultAsync(l => l.StreamStatus == 1);
            if (liveStream == null) return NotFound("目前沒有正在進行的直播。");
            liveStream.StreamStatus = 2;
            liveStream.EndedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok("直播已成功結束。");
        }

        // GET: /api/streaming/current - 任何人檢查當前是否有直播
        [HttpGet("current")]
        public async Task<ActionResult<CurrentStreamDto>> GetCurrentStream()
        {
            var liveStream = await _context.Livestreams.FirstOrDefaultAsync(l => l.StreamStatus == 1);

            // 【★ 核心邏輯 ★】
            // 邏輯非常單純：有直播就回傳資料，沒直播就回傳 null。
            if (liveStream == null)
            {
                return Ok(null);
            }

            return Ok(new CurrentStreamDto
            {
                Title = liveStream.Title,
                PlaybackUrl = liveStream.IvsPlaybackUrl,
                ChatRoomArn = _configuration["IVS:ChatRoomArn"]
            });
        }

        // POST: /api/streaming/chat-token - 【新】為使用者產生聊天室的臨時通行證 (手動建立版)
        [HttpPost("chat-token")]
        public async Task<ActionResult<string>> GetChatToken()
        {
            var userId = "user-" + Guid.NewGuid().ToString();
            var chatRoomArn = _configuration["IVS:ChatRoomArn"];
            if (string.IsNullOrEmpty(chatRoomArn)) return StatusCode(500, "IVS Chat Room ARN 未設定。");

            try
            {
                // 手動從 appsettings.json 讀取 AWS 金鑰和區域
                var awsOptions = _configuration.GetSection("AWS");
                var accessKey = awsOptions["AccessKeyId"];
                var secretKey = awsOptions["SecretAccessKey"];
                var region = RegionEndpoint.GetBySystemName(awsOptions["Region"]);
                var credentials = new BasicAWSCredentials(accessKey, secretKey);

                // 使用讀取到的金鑰和區域，手動建立 IVSChat 客戶端
                var chatClient = new AmazonIvschatClient(credentials, region);

                var request = new CreateChatTokenRequest
                {
                    RoomIdentifier = chatRoomArn,
                    UserId = userId,
                    Capabilities = new List<string> { "SEND_MESSAGE", "DELETE_MESSAGE" },
                    SessionDurationInMinutes = 120
                };

                var response = await chatClient.CreateChatTokenAsync(request);
                return Ok(response.Token);
            }
            catch (Exception ex)
            {
                // 加上更詳細的日誌，方便未來偵錯~
                Console.WriteLine($"產生 Chat Token 失敗: {ex.ToString()}");
                return StatusCode(500, $"產生 Chat Token 失敗: {ex.Message}");
            }
        }
    }
}
