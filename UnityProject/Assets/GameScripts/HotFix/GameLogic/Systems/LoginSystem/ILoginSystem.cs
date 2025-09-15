using Cysharp.Threading.Tasks;

namespace GameLogic
{
    interface ILoginSystem : ISystem
    {
        UniTask Login(string account, string password);
        UniTask SignUp(string account, string password);
    }
}