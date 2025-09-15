using TEngine;

namespace GameLogic
{
    [System(1)]
    public class LoginSystem : ISystem
    {
        public void OnInit()
        {
            Log.Info("LoginSystem OnInit");
        }

        public void OnStart()
        {
            Log.Info("LoginSystem OnStart");
        }
        
        public void OnDestroy()
        {
            Log.Info("LoginSystem OnDestroy");
        }
    }
}