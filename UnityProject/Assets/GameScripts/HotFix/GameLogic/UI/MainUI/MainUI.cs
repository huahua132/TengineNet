using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.UI)]
	class MainUI : UIWindow
	{
		#region 脚本工具生成的代码
		private Button _btnEmail;
		protected override void ScriptGenerator()
		{
			_btnEmail = FindChildComponent<Button>("leftBtns/m_btnEmail");
			_btnEmail.onClick.AddListener(OnClickEmailBtn);
		}
		#endregion

		#region 事件
		private void OnClickEmailBtn()
		{
		}
		#endregion

	}
}
