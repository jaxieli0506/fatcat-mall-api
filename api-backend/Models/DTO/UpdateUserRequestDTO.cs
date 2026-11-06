using System.ComponentModel.DataAnnotations;
namespace api_backend.Models.DTO
{
    public class UpdateUserRequestDTO
    {
        public string? Name { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public int? Gender { get; set; }

        public DateTime? Birthdate { get; set; }

        // 頭像路徑
        public string? PicPath { get; set; }
    }
}
