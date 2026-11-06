namespace api_backend.Models.DTO
{
    public class RegisterRequestDTO
    {
        public string Name { get; set; }            
        public string Email { get; set; }           
        public string Password { get; set; }
        public string? Phone { get; set; }
        public int? Gender { get; set; } // 0: 男, 1: 女, 2: 其他, null: 未指定
        public DateTime? Birthdate { get; set; } 
    }
}
