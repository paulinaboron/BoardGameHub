namespace BoardGameHub.Models.DbModels
{
    public enum GameStatus
    {
        Dostępna,
        Wypożyczona,
        W_konserwacji
    }
    public class BoardGame
    {
        public int BoardGameId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }

        public string? ImagePath { get; set; }
        public GameStatus Status { get; set; }

        public int PublisherId { get; set; }
        public virtual Publisher? Publisher { get; set; }

        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public BoardGame() { }

        public BoardGame(int boardGameId, string title, GameStatus status)
        {
            BoardGameId = boardGameId;
            Title = title;
            Status = status;
        }
    }
}

