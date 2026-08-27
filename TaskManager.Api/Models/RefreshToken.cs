namespace TaskManager.Api.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}