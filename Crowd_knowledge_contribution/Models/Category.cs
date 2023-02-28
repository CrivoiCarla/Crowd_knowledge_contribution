using System.ComponentModel.DataAnnotations;

namespace Crowd_knowledge_contribution.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele categoriei este obligatoriu")]
        public string CategoryName { get; set; }

        public virtual ICollection<Article> Articles { get; set; }
    }

}
