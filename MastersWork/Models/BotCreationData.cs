using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MastersWork.Models
{
    public class BotCreationData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ChatId { get; set; }
        public string? BotName { get; set; }
        public string? Token { get; set; }
        public bool? IsCompleted { get; set; }
        public bool? IsBotWorking { get; set; }
        public List<QuestionAnswer>? QA { get; set; } = [];
    }

    public class QuestionAnswer
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
    }
}
