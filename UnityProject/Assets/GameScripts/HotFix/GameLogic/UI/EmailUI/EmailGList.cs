using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
	public enum EMAIL_TYPE
	{
		G = 1,
		S = 2,
		F = 3,
	}

	public class EmailDataGeter : ILoopScrollDataGeter
	{
		private SortedList<(long createTime, long guid), hallserver_email.oneEmail> _sortList;
		public void SetData(SortedList<(long createTime, long guid), hallserver_email.oneEmail> sortList)
		{
			_sortList = sortList;
		}
		public T GetData<T>(int index)
		{
			return (T)(object)_sortList.Values[index];
		}
	}

	public class EmailCell : LoopCellBase
	{
		private Text _titile;
        protected override void OnInit()
		{
			_titile = _Trf.Find("m_textTitle").GetComponent<Text>();
		}
		protected override void OnRefresh(int index)
		{
			var emailData = _DataGeter.GetData<hallserver_email.oneEmail>(index);
			_titile.text = emailData.title;
		}
	}

	[Window(UILayer.UI)]
	class EmailGList : UIWidget
	{
		private EmailDataGeter _dataGeter;
		private LoopVerticalScrollRect _emailList;
		private LoopScrollInitOnStart _loopInit;
		private IEmailSystem _iemail;
		#region 脚本工具生成的代码
		protected override void ScriptGenerator()
		{
			_dataGeter = new EmailDataGeter();
			_emailList = gameObject.GetComponent<LoopVerticalScrollRect>();
			_iemail = GameModule.System.GetSystem<IEmailSystem>();
			_loopInit = gameObject.GetComponent<LoopScrollInitOnStart>();
			_loopInit.Init(CellCreater, _dataGeter);
		}
		#endregion

		private LoopCellBase CellCreater()
		{
			return new EmailCell();
		}

		#region 事件
		#endregion

		public void Switch(EMAIL_TYPE type)
		{
			var emailDataList = _iemail.GetEmailsByType((int)type);
			_dataGeter.SetData(emailDataList);
			_emailList.totalCount = emailDataList.Count;
			_emailList.RefillCells();
		}
	}
}
