using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.Top)]
	class CommonPopupUI : UIWindow
	{
        #region 脚本工具生成的代码
        public Transform Popup;
        protected override void ScriptGenerator()
        {
            Popup = FindChild("Popup");
			Popup.gameObject.SetActive(false);
        }
		#endregion

		#region 事件
		#endregion

	}
}
