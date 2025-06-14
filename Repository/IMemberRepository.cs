using MovieTheater.Models;
using System.Collections.Generic;

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
        public Member? GetByIdentityCard(string identityCard);
        public Member? GetByAccountId(string accountId);
        public Member GetByMemberId(string memberId);
    }
}
