using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class SettingsUI : UIWindow
    {
        #region 脚本工具生成的代码
        private Button _btnClose;
        private Dropdown _m_Dropdown;
        protected override void ScriptGenerator()
        {
            _btnClose = FindChildComponent<Button>("m_btnClose");
            _m_Dropdown = FindChildComponent<Dropdown>("layout/m_Dropdown");
            _m_Dropdown.onValueChanged.AddListener(onValueChanged);
            _btnClose.onClick.AddListener(OnClickCloseBtn);
        }
        #endregion

        #region 事件
        private void OnClickCloseBtn()
        {
            Hide();
        }

        private void onValueChanged(int value) {
            if (value == 0)
            {
                GameModule.Localization.SetLanguage(Language.ChineseSimplified);
            }
            else
            {
                GameModule.Localization.SetLanguage(Language.English);
            }
        }
		#endregion

    }
}
