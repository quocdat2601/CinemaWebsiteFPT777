using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IPromotionRepository
    {
        public IEnumerable<Promotion> GetAll();
        public Promotion? GetById(int id);
        public void Add(Promotion promotion);
        public void Update(Promotion promotion);
        public void Delete(int id);
        public void Save();

    }
}
