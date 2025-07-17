using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Repository
{
    public interface IVersionRepository
    {
        public IEnumerable<Models.Version> GetAll();

        public Models.Version? GetById(int id);

        public void Add(Models.Version version);

        public void Update(Models.Version version);

        public bool Delete(int id);

        public void Save();
    }
}
