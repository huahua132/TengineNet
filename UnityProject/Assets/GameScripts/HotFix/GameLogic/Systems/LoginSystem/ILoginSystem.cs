using Cysharp.Threading.Tasks;

namespace GameLogic
{
    interface ILoginSystem: ISystem
    {
        UniTask Login(string account, string password);
    }
}