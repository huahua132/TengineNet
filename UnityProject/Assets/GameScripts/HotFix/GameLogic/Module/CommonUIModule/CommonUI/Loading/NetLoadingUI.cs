using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.UI)]
	class NetLoadingUI : UIWindow
    {
        private float rotateSpeed = 200f; // 旋转速度
		#region 脚本工具生成的代码
		private Image _imgLoad;
        protected override void ScriptGenerator()
        {
            _imgLoad = FindChildComponent<Image>("m_imgLoad");
        }
        #endregion

        protected override void OnUpdate()
        {
            _imgLoad.transform.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
        }

		#region 事件
		#endregion
	}
}
