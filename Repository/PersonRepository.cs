using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class PersonRepository : IPersonRepository
    {
        private readonly MovieTheaterContext _context;

        public PersonRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public IEnumerable<Person> GetAll()
        {
            return _context.People.ToList();
        }

        public Person? GetById(int personId)
        {
            return _context.People
                .FirstOrDefault(m => m.PersonId == personId);
        }

        public IEnumerable<Person> GetDirectors()
        {
            return _context.People.Where(p => p.IsDirector == true).ToList();
        }

        public IEnumerable<Person> GetActors()
        {
            return _context.People.Where(p => p.IsDirector == false).ToList();
        }

        public void Add(Person person)
        {
            _context.People.Add(person);
        }

        public void Update(Person person)
        {
            var existingPerson = _context.People.Find(person.PersonId);
            if (existingPerson != null)
            {
                _context.Entry(existingPerson).CurrentValues.SetValues(person);
            }
        }

        public void Delete(int personId)
        {
            var person = _context.People.Find(personId);
            if (person != null)
            {
                _context.People.Remove(person);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
