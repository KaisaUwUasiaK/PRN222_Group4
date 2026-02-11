namespace PRN222_Group4.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
