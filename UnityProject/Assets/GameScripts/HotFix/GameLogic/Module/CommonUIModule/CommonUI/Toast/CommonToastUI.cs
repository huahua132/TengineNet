using UnityEngine;

namespace GameLogic
{
	[Window(UILayer.Tips)]
	class CommonToastUI : UIWindow
	{
		#region 脚本工具生成的代码
		public Transform Toast;
		protected override void ScriptGenerator()
		{
			Toast = FindChild("Toast");
			Toast.gameObject.SetActive(false);
		}
		#endregion

		#region 事件
		#endregion

	}
}
