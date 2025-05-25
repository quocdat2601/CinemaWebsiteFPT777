using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IMemberRepository
    {
        public IEnumerable<Member> GetAll();
        public Member? GetById(string memberId);
        public string GenerateMemberId();
        public void Add(Member member);
        public void Update(Member member);
        public void Delete(string memberId);
        public void Save();

    }
}
