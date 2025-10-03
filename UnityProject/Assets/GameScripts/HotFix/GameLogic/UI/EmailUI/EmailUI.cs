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
        private Transform _redDotGp;
        private Transform _redDotSp;
        private Transform _redDotFp;
        private IRedDot _redDotG;
        private IRedDot _redDotS;
        private IRedDot _redDotF;

        protected override void ScriptGenerator()
        {
            _imgBg = FindChildComponent<Button>("m_imgBg");
            _group = FindChildComponent<ToggleGroup>("m_group");
            _toggleG = FindChildComponent<Toggle>("m_group/m_toggleG");
            _toggleS = FindChildComponent<Toggle>("m_group/m_toggleS");
            _toggleF = FindChildComponent<Toggle>("m_group/m_toggleF");
            _redDotGp = FindChildComponent<Transform>("m_group/m_toggleG/RedDotG");
            _redDotSp = FindChildComponent<Transform>("m_group/m_toggleS/RedDotS");
            _redDotFp = FindChildComponent<Transform>("m_group/m_toggleF/RedDotF");
            _toggleG.onValueChanged.AddListener(OnToggleGChange);
            _toggleS.onValueChanged.AddListener(OnToggleSChange);
            _toggleF.onValueChanged.AddListener(OnToggleFChange);
            GetGlist();
            OnToggleGChange(_toggleG.isOn);
            OnToggleGChange(_toggleS.isOn);
            OnToggleGChange(_toggleF.isOn);
            _imgBg.onClick.AddListener(OnClickImgBgBtn);

            GameModule.CommonUI.GetRedDot(RedDotWordDefine.GlobalEmail, RedDotType.RED_NUM, OnLoadRedDotSuccG);
            GameModule.CommonUI.GetRedDot(RedDotWordDefine.SysEmail, RedDotType.RED_NUM, OnLoadRedDotSuccS);
            GameModule.CommonUI.GetRedDot(RedDotWordDefine.FriendEmail, RedDotType.RED_NUM, OnLoadRedDotSuccF);
        }
        #endregion

        private void OnLoadRedDotSuccG(IRedDot redDot)
        {
            _redDotG = redDot;
            redDot.SetParent(_redDotGp.gameObject);
        }

        private void OnLoadRedDotSuccS(IRedDot redDot)
        {
            _redDotS = redDot;
            redDot.SetParent(_redDotSp.gameObject);
        }

        private void OnLoadRedDotSuccF(IRedDot redDot)
        {
            _redDotF = redDot;
            redDot.SetParent(_redDotFp.gameObject);
        }

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

        #region 事件
        protected override void OnDestroy()
        {
            if (_redDotG != null)
            {
                _redDotG.SetRecycle();
            }
            if (_redDotS != null)
            {
                _redDotS.SetRecycle();
            }
            if (_redDotF != null)
            {
                _redDotF.SetRecycle();
            }
        }
        #endregion
    }
}
