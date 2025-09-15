using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class LoginUI : UIWindow
    {
        private ILoginSystem _loginSystem;

        #region 脚本工具生成的代码
        private InputField _inputAccount;
        private InputField _inputPassword;
        private Button _btnLogin;
        private Button _btnSignUp;
        protected override void ScriptGenerator()
        {
            _inputAccount = FindChildComponent<InputField>("m_inputAccount");
            _inputPassword = FindChildComponent<InputField>("m_inputPassword");
            _btnLogin = FindChildComponent<Button>("m_btnLogin");
            _btnLogin.onClick.AddListener(OnClickLoginBtn);
            _btnSignUp = FindChildComponent<Button>("m_btnSignUp");
            _btnSignUp.onClick.AddListener(OnClickSignUpBtn);
            
            _loginSystem = GameModule.System.GetSystem<ILoginSystem>();
        }
        #endregion

        #region 事件
        private void OnClickLoginBtn()
        {
            string account = _inputAccount.text;
            string password = _inputPassword.text;
            _loginSystem.Login(account, password);
        }

        private void OnClickSignUpBtn()
        {
            string account = _inputAccount.text;
            string password = _inputPassword.text;
            _loginSystem.SignUp(account, password);
        }
		#endregion

    }
}
