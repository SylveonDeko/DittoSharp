using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

[Table("messages")]
public class Message
{
    [Key] [Column("message_id")] public int MessageId { get; set; }

    [Column("author_id")] [Required] public ulong AuthorId { get; set; }

    [Column("content")] [Required] public string Content { get; set; } = null!;

    [Column("timestamp")] public DateTime? Timestamp { get; set; }

    [Column("is_system_message")] public bool? IsSystemMessage { get; set; }

    [Column("is_read")] public bool? IsRead { get; set; }

    [Column("title")] public string? Title { get; set; }

    [Column("inbox")] public ulong? InboxId { get; set; }
}