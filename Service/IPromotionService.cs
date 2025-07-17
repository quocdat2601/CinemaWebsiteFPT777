using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IPromotionService
    {
        public IEnumerable<Promotion> GetAll();
        public Promotion? GetById(int id);
        public bool Add(Promotion promotion);
       public bool Update(Promotion promotion);
        public bool Delete(int id);
        public void Save();

        Promotion? GetBestPromotionForShowDate(DateOnly showDate);
        List<Promotion> GetEligiblePromotionsForMember(string memberId, int seatCount = 0, DateTime? showDate = null, string movieId = null, string movieName = null);
    }
}
