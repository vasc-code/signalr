namespace SignalRPoc.Models
{
    public class UserSession
    {
        public string Email { get; }
        public string Name { get; }
        public string Image { get; }

        public UserSession(string email, string name, string image)
        {
            Email = email;
            Name = name;
            Image = image;
        }
    }
}
