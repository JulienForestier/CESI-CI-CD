namespace CESI_CI_CD.ApiService.Data.Entities;

public class Message
{
    public Guid Id { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public Guid SenderId { get; set; }
    public User? Sender { get; set; }
}
