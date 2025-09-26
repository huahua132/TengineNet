using TEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    [System((int)SystemPriority.HallMain)]
    public class HallMainSystem : IHallMainSystem
    {
        public void OnInit()
        {
            GameModule.System.AddEvent<bool>(ILoginLogic_Event.HallLoginAuthSuccess, OnLoginAuthSuccess);
        }

        public void OnStart()
        {
        }

        public void OnDestroy()
        {

        }

        private void OnLoginAuthSuccess(bool isReconnect)
        {
            GameEvent.Get<ILoginUI>().CloseLoginUI();
            GameModule.UI.ShowUI<MainUI>();
        }
    }
}