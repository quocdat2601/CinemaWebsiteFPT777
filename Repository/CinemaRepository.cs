using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class CinemaRepository : ICinemaRepository
    {
        private readonly MovieTheaterContext _context;
        public CinemaRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public IEnumerable<CinemaRoom> GetAll()
        {
            return _context.CinemaRooms.ToList();
        }

        public CinemaRoom? GetById(int? id)
        {
            return _context.CinemaRooms.FirstOrDefault(a => a.CinemaRoomId == id);
        }

        public void Add(CinemaRoom cinemaRoom)
        {
            _context.CinemaRooms.Add(cinemaRoom);
        }

        public void Update(CinemaRoom cinemaRoom)
        {
            var existingCinema = _context.CinemaRooms.FirstOrDefault(m => m.CinemaRoomId == cinemaRoom.CinemaRoomId);
            if (existingCinema != null)
            {
                existingCinema.CinemaRoomName = cinemaRoom.CinemaRoomName;
                existingCinema.SeatQuantity = cinemaRoom.SeatQuantity;
                _context.SaveChanges();
            }
        }
        public void Delete(int id)
        {
            var cinemaRoom = _context.CinemaRooms.FirstOrDefault(m => m.CinemaRoomId == id);
            if (cinemaRoom != null)
            {
                _context.CinemaRooms.Remove(cinemaRoom);
            }
        }
        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
