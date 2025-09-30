using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TEngine;
using GameConfig;

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
		private Text _isRead;
		private Text _tooggleShow;
		private Toggle _toggle;
		private RectTransform _bottom;
		private Text _content;
		private LoopHorizontalScrollRect _rewardList;
		private LoopScrollInitOnStart _loopInit;
		private EmailRewardDataGeter _dataGeter;
		protected override void OnInit()
		{
			_titile = _Trf.Find("head/m_textTitle").GetComponent<Text>();
			_isRead = _Trf.Find("head/m_textReadFlag").GetComponent<Text>();
			_tooggleShow = _Trf.Find("head/m_toggleShow").GetComponentInChildren<Text>();
			_toggle = _Trf.Find("head/m_toggleShow").GetComponent<Toggle>();
			_toggle.onValueChanged.AddListener(OnToggleChange);

			_bottom = _Trf.Find("bottom").GetComponent<RectTransform>();
			_content = _Trf.Find("bottom/m_textContent").GetComponent<Text>();
			_rewardList = _Trf.Find("bottom/ItemList").GetComponent<LoopHorizontalScrollRect>();
			_loopInit = _Trf.Find("bottom/ItemList").GetComponent<LoopScrollInitOnStart>();
			_dataGeter = new EmailRewardDataGeter();
			_loopInit.Init(CellCreater, _dataGeter);
		}
		private static LoopCellBase CellCreater()
		{
			return new EmailRewardCell();
		}
		protected override void OnRefresh(int index)
		{
			var emailData = _DataGeter.GetData<hallserver_email.oneEmail>(index);
			_titile.text = emailData.title;
			_isRead.text = emailData.read_flag == 1 ? "已读" : "未读";
			_tooggleShow.text = _toggle.isOn ? "收拢" : "展开";
			_bottom.gameObject.SetActive(_toggle.isOn);
			_content.text = emailData.content;
			_dataGeter.SetData(emailData.item_list);
			_rewardList.totalCount = emailData.item_list.Count;
			_rewardList.RefillCells();
		}
		private void OnToggleChange(bool isOn)
		{
			_tooggleShow.text = _toggle.isOn ? "收拢" : "展开";
			_bottom.gameObject.SetActive(_toggle.isOn);
		}
	}

	public class EmailRewardDataGeter : ILoopScrollDataGeter
	{
		private List<common.Item> _itemList;
		public void SetData(List<common.Item> itemList)
		{
			_itemList = itemList;
		}

		public T GetData<T>(int index)
		{
			return (T)(object)_itemList[index];
		}
	}

	public class EmailRewardCell : LoopCellBase
	{
		private IItemIcon _IitemIcon;
		private int _index;

		private void OnItemCoinLoadSucc(IItemIcon itemIcon)
		{
			//Log.Info($"OnItemCoinLoadSucc >>> {itemIcon}");
			_IitemIcon = itemIcon;
			if (_index >= 0)
			{
				OnRefresh(_index);
			}
			itemIcon.SetParent(_Trf.gameObject);
		}
		protected override void OnInit()
		{
			GameModule.CommonUI.GetItemIcon(OnItemCoinLoadSucc);
		}

		protected override void OnRefresh(int index)
		{
			//Log.Info($"OnRefresh >>> {_index}");
			_index = index;
			if (_IitemIcon == null) return;
			var itemData = _DataGeter.GetData<common.Item>(index);
			var itemID = itemData.id;
			var itemCount = itemData.count;
			_IitemIcon.SetItemID((item_ID)itemID);
			_IitemIcon.SetItemNum((int)itemCount);
		}

		protected override void OnRecycle()
		{
			_index = -1;
		}

		protected override void OnRelease()
		{
			if (_IitemIcon != null)
			{
				_IitemIcon.SetRecycle();
				_IitemIcon = null;
			}
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
