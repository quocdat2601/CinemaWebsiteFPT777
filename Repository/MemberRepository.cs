using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class MemberRepository : IMemberRepository

    {
        private readonly MovieTheaterContext _context;

        public MemberRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public IEnumerable<Member> GetAll()
        {
            return _context.Members.Include(m => m.Account).ToList();
        }


        public Member? GetById(string memberId)
        {
            return _context.Members.FirstOrDefault(m => m.MemberId == memberId);
        }

        public string GenerateMemberId()
        {
            var latestMember = _context.Members
                .OrderByDescending(a => a.MemberId)
                .FirstOrDefault();

            if (latestMember == null)
            {
                return "MB001";
            }

            if (int.TryParse(latestMember.MemberId.Substring(2, 3), out int number))
            {
                return $"MB{(number + 1):D3}";
            }

            return $"MB{DateTime.Now:yyyyMMddHHmmss}";
        }

        public void Add(Member member)
        {
            if (string.IsNullOrEmpty(member.MemberId))
            {
                member.MemberId = GenerateMemberId();
            }
            _context.Members.Add(member);
        }

        public void Update(Member member)
        {
            _context.Members.Update(member);
            _context.SaveChanges();
        }

        public void Delete(string memberId)
        {
            var member = _context.Members.Find(memberId);
            if (member != null)
            {
                _context.Members.Remove(member);
                _context.SaveChanges();
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public Member GetByIdentityCard(string identityCard)
        {
            return _context.Members
                .Include(m => m.Account)
                .FirstOrDefault(m => m.Account.IdentityCard == identityCard);
        }

        public Member GetByAccountId(string accountId)
        {
            return _context.Members
                .Include(m => m.Account)
                .FirstOrDefault(m => m.AccountId == accountId);
        }

        public Member GetByMemberId(string memberId)
        {
            return _context.Members
                .Include(m => m.Account)
                .FirstOrDefault(m => m.MemberId == memberId);
        }
    }
}
