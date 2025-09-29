using UnityEngine;
using UnityEngine.UI;
using TEngine;
using System;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class EmailUI : UIWindow
    {
        #region 脚本工具生成的代码
        private Button _imgBg;
        private ToggleGroup _group;
        private Toggle _toggleG;
        private Toggle _toggleS;
        private Toggle _toggleF;
        private EmailGList _gList;
        protected override void ScriptGenerator()
        {
            _imgBg = FindChildComponent<Button>("m_imgBg");
            _group = FindChildComponent<ToggleGroup>("m_group");
            _toggleG = FindChildComponent<Toggle>("m_group/m_toggleG");
            _toggleS = FindChildComponent<Toggle>("m_group/m_toggleS");
            _toggleF = FindChildComponent<Toggle>("m_group/m_toggleF");
            _toggleG.onValueChanged.AddListener(OnToggleGChange);
            _toggleS.onValueChanged.AddListener(OnToggleSChange);
            _toggleF.onValueChanged.AddListener(OnToggleFChange);
            GetGlist();
            OnToggleGChange(_toggleG.isOn);
            OnToggleGChange(_toggleS.isOn);
            OnToggleGChange(_toggleF.isOn);
            _imgBg.onClick.AddListener(OnClickImgBgBtn);
        }
        #endregion

        private void GetGlist()
        {
            if (_gList == null)
            {
                _gList = CreateWidget<EmailGList>("scrollG", true);
            }
        }

        #region 事件
        private void OnToggleGChange(bool isOn)
        {
            if (!isOn) return;
            _gList.Switch(EMAIL_TYPE.G);
        }
        private void OnToggleSChange(bool isOn)
        {
            if (!isOn) return;
            _gList.Switch(EMAIL_TYPE.S);
        }
        private void OnToggleFChange(bool isOn)
        {
            if (!isOn) return;
            _gList.Switch(EMAIL_TYPE.F);
        }

        private void OnClickImgBgBtn()
        {
            Hide();
        }
		#endregion

    }
}
