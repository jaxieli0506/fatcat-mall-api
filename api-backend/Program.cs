using api_backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using api_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//註冊TeamDb2 資料內容類別到DI容器
builder.Services.AddDbContext<TeamDb2Context>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("TeamDb2"));
});


//設定CORS權限(先做全開放，後面再一步步限縮)
string CorsPolicyName = "AllowAllOrigins";  //權限名稱 AllowAllOrigins
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CorsPolicyName, 
        builder => 
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();        
        });
    //signalr專用
    options.AddPolicy(name: "SignalRCorsPolicy",
       policy =>
       {
           policy.WithOrigins("http://localhost:5180", "https://localhost:7017")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials(); // 允許認證資訊
       });
});


//設定JWT認證
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        // 【讓 JWT 驗證也能處理 SignalR 的 Token 】       
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // 先檢查一般使用者的 token
                var accessToken = context.Request.Query["access_token"];

                // 再檢查管理者的 token
                var adminToken = context.Request.Query["admin_token"];

                var path = context.HttpContext.Request.Path;

                // 只要路徑是 chatHub，就進行檢查
                if (path.StartsWithSegments("/chatHub", StringComparison.OrdinalIgnoreCase))
                {
                    // 【修改處】優先使用 accessToken
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    // 如果沒有 accessToken，再檢查 adminToken
                    else if (!string.IsNullOrEmpty(adminToken))
                    {
                        context.Token = adminToken;
                    }
                }
                return Task.CompletedTask;
            }
        };
    });


// 添加 SignalR
builder.Services.AddSignalR();

//註冊郵件服務 (必須在 AuthService 之前註冊)
builder.Services.AddScoped<IEmailService, EmailService>();

//註冊JWT自定義服務
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>(); // rr 註冊管理員驗證服務
builder.Services.AddScoped<JwtService>();

//註冊第三方登入服務
builder.Services.AddScoped<IExternalLoginService, ExternalLoginService>();

//註冊地址管理服務
builder.Services.AddScoped<IAddressService, AddressService>();



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient(); // 註冊 HttpClient(為了使用綠界API) by JJ 


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
    

//套用CORS權限(在program.cs套用，會影響專案所有的api控制器)
app.UseCors(CorsPolicyName);
app.UseHttpsRedirection();

// 靜態檔案必須在認證之前
app.UseStaticFiles(); //讓 wwwroot 資料夾裡的圖可以被瀏覽器讀到

app.UseAuthentication(); //JWT認證
app.UseAuthorization(); 


app.MapControllers();


// 添加 SignalR Hub 路由將 "SignalRCorsPolicy" 應用到這個 Hub 上
app.MapHub<api_backend.Hubs.ChatHub>("/chatHub")
   .RequireCors("SignalRCorsPolicy");


// 添加管理員後台路由
app.MapGet("/admin", () => Results.Redirect("/admin.html"));


app.Run();
