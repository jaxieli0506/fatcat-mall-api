using Microsoft.EntityFrameworkCore;

namespace api_backend.Models
{
    public partial class TeamDb2Context : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //判斷optionBuilder是否已被設定過，如果未設定，才進行後續設定，避免重複設定
            if (!optionsBuilder.IsConfigured) {
                IConfiguration Config = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("appsettings.json").Build();
                optionsBuilder.UseSqlServer(Config.GetConnectionString("TeamDb2"));
            }
        }
    }
}
