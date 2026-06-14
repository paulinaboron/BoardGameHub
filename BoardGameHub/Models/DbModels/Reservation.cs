using System;

namespace BoardGameHub.Models.DbModels
{
    public class Reservation
    {
        public int ReservationId { get; set; }
        public int BoardGameId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; } = DateTime.Now;

        public BoardGame? BoardGame { get; set; }
    }
}