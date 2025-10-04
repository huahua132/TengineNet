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
		private Button _btnSettings;
		private Transform _redDotCon;
		private IRedDot _redDot;
		protected override void ScriptGenerator()
		{
			_btnEmail = FindChildComponent<Button>("leftBtns/m_btnEmail");
			_btnSettings = FindChildComponent<Button>("leftBtns/m_btnSettings");
			_redDotCon = FindChild("leftBtns/m_btnEmail/RedDotCon");
			GameModule.CommonUI.GetRedDot(RedDotWordDefine.EmailBtn, RedDotType.RED_NUM, OnLoadSuccRedDot);
			_btnEmail.onClick.AddListener(OnClickEmailBtn);
			_btnSettings.onClick.AddListener(OnClickSettingBtn);
		}
		#endregion

		private void OnLoadSuccRedDot(IRedDot redDot)
		{
			_redDot = redDot;
			_redDot.SetParent(_redDotCon.gameObject);
		}

		#region 事件
		private void OnClickEmailBtn()
		{
			GameModule.UI.ShowUI<EmailUI>();
		}

		private void OnClickSettingBtn()
		{
			GameModule.UI.ShowUI<SettingsUI>();
		}
		#endregion

		#region 动作
		protected override void OnDestroy()
		{
			if (_redDot != null)
			{
				_redDot.SetRecycle();
			}
		}
		#endregion
	}
}
