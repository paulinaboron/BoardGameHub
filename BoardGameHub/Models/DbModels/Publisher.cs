namespace BoardGameHub.Models.DbModels
{
    public class Publisher
    {
        public int PublisherId { get; set; }
        public string Name { get; set; }

        public virtual List<BoardGame> BoardGames { get; set; } = new List<BoardGame>();

        public Publisher() { }
    }
}
