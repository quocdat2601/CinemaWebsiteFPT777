using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IPersonRepository
    {
        public IEnumerable<Person> GetAll();
        public Person? GetById(int personId);
        public IEnumerable<Person> GetDirectors();
        public IEnumerable<Person> GetActors();
        public void Add(Person person);
        public void Update(Person person);
        public void Delete(int personId);
        public void Save();
        public IEnumerable<Movie> GetMovieByPerson(int personId);
    }
}
