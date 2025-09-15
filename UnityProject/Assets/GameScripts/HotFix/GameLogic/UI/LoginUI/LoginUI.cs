using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.UI)]
	class LoginUI : UIWindow
	{
		#region 脚本工具生成的代码
		private InputField _inputAccount;
		private InputField _inputPassword;
		private Button _btnLogin;
		protected override void ScriptGenerator()
		{
			_inputAccount = FindChildComponent<InputField>("m_inputAccount");
			_inputPassword = FindChildComponent<InputField>("m_inputPassword");
			_btnLogin = FindChildComponent<Button>("m_btnLogin");
			_btnLogin.onClick.AddListener(OnClickLoginBtn);
		}
        #endregion

        #region 事件
        private void OnClickLoginBtn()
        {
            string account = _inputAccount.text;
            string password = _inputPassword.text;
            var loginSystem = GameModule.System.GetSystem<ILoginSystem>();
            loginSystem.Login(account, password);
		}
		#endregion

	}
}
