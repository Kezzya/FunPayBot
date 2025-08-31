namespace FunPayBot.src.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string FunPayUserId { get; private set; }
        public string Username { get; private set; }

        public User(string funPayUserId, string username)
        {
            Id = Guid.NewGuid();
            FunPayUserId = funPayUserId ?? throw new ArgumentNullException(nameof(funPayUserId));
            Username = username ?? throw new ArgumentNullException(nameof(username));
        }
    }
}