namespace FunPayBot.src.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; private set; }
        public string FunPayMessageId { get; private set; }
        public User Sender { get; private set; }
        public string Content { get; private set; }
        public DateTime SentAt { get; private set; }
        public string ChatId { get; private set; }
        public Message()
        {
            
        }
        public Message(string funPayMessageId, User sender, string content, string chatId, DateTime sentAt)
        {
            Id = Guid.NewGuid();
            FunPayMessageId = funPayMessageId ?? throw new ArgumentNullException(nameof(funPayMessageId));
            Sender = sender ?? throw new ArgumentNullException(nameof(sender));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            ChatId = chatId ?? throw new ArgumentNullException(nameof(chatId));
            SentAt = sentAt;
        }
    }
}