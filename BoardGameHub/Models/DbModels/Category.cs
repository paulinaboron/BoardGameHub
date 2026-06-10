namespace BoardGameHub.Models.DbModels
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }

        public virtual List<BoardGame> BoardGames { get; set; } = new List<BoardGame>();

        public Category() { }
    }
}
