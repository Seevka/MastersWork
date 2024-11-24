using MastersWork.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MastersWork.Models
{
    public class UserState
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ChatId { get; set; }
        public string? UserName { get; set; }
        public bool IsAdmin { get; set; }
        public BotCreationStep CurrentStep { get; set; }
        public string? TempData { get; set; }
    }
}
