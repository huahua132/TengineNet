using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	public enum EMAIL_TYPE
	{
		G,
		S,
		F,
	}

	[Window(UILayer.UI)]
	class EmailGList : UIWidget
	{
		private LoopVerticalScrollRect _emailList;
		#region 脚本工具生成的代码
		protected override void ScriptGenerator()
		{
			_emailList = gameObject.GetComponent<LoopVerticalScrollRect>();
		}
		#endregion

		#region 事件
		#endregion

		public void Switch(EMAIL_TYPE type)
		{
			_emailList.totalCount = 0;
		}
	}
}
