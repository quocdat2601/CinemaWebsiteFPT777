using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class VersionRepository : IVersionRepository
    {
    
        private readonly MovieTheaterContext _context;

        public VersionRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public IEnumerable<Models.Version> GetAll()
        {
            return _context.Versions
                .ToList();
        }


        public Models.Version? GetById(int id)
        {
            return _context.Versions.FirstOrDefault(m => m.VersionId == id);
        }

        public void Add(Models.Version version)
        {
            _context.Versions.Add(version);
        }

        public void Update(Models.Version version)
        {
            _context.Versions.Update(version);
            _context.SaveChanges();
        }

        public bool Delete(int id)
        {
            var version = _context.Versions.Find(id);
            if (version != null)
            {
                _context.Versions.Remove(version);
                _context.SaveChanges();
            }
            return true;
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
