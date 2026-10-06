namespace TonKhoApp.Domain.Entities
{
    public class UserRole
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RoleId { get; set; }

        public DateTime AssignedAt { get; set; }
        public int? AssignedBy { get; set; }

        // Navigation
        public User? User { get; set; }
        public Role? Role { get; set; }
        public User? AssignedByUser { get; set; }
    }
}
