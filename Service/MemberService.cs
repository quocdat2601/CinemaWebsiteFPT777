using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _memberRepository;

        public MemberService(IMemberRepository memberRepository)
        {
            _memberRepository = memberRepository;
        }

        public Member? GetById(string memberId)
        {
            return _memberRepository.GetById(memberId);
        }

        public Member? GetByAccountId(string accountId)
        {
            return _memberRepository.GetByAccountId(accountId);
        }

        public Member? GetByMemberId(string memberId)
        {
            return _memberRepository.GetByMemberId(memberId);
        }

        public Member? GetByIdWithAccount(string memberId)
        {
            return _memberRepository.GetByIdWithAccount(memberId);
        }

        public Member? GetByIdWithAccountAndRank(string accountId)
        {
            return _memberRepository.GetByAccountId(accountId);
        }

        public IEnumerable<Member> GetAll()
        {
            return _memberRepository.GetAll();
        }

        public void Add(Member member)
        {
            _memberRepository.Add(member);
        }

        public void Update(Member member)
        {
            _memberRepository.Update(member);
        }

        public void Delete(string memberId)
        {
            _memberRepository.Delete(memberId);
        }

        public void Save()
        {
            _memberRepository.Save();
        }
    }
} 