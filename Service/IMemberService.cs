using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IMemberService
    {
        Member? GetById(string memberId);
        Member? GetByAccountId(string accountId);
        Member? GetByMemberId(string memberId);
        Member? GetByIdWithAccount(string memberId);
        Member? GetByIdWithAccountAndRank(string accountId);
        IEnumerable<Member> GetAll();
        void Add(Member member);
        void Update(Member member);
        void Delete(string memberId);
        void Save();
    }
}