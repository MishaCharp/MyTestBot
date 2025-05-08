using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RevGameCaptchaNotifier
{
    [Table("keys")]
    public class KeyModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("key")]
        public string? Key { get; set; }

        [Column("chat_id")]
        public string? ChatId { get; set; }

        [Column("activated")]
        public bool Activated { get; set; }
    }
}
