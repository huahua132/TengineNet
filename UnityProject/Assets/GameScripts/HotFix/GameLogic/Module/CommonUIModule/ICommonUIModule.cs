using System;
using UnityEngine;
using GameConfig;

namespace GameLogic
{
    public interface ICommonUIModule
    {
        #region Toast提示
        void ShowToast(string txt);
        #endregion

        #region Popup弹窗
        /// <summary>
        /// 显示确认弹窗（有取消和确定按钮）
        /// </summary>
        public void ShowConfirm(string title, string content, Action onConfirm = null, Action onCancel = null, string confirmText = "确定", string cancelText = "取消");
        /// <summary>
        /// 显示提示弹窗（只有确定按钮）
        /// </summary>
        public void ShowAlert(string title, string content, Action onConfirm = null, string confirmText = "确定");
        /// <summary>
        /// 显示自定义弹窗
        /// </summary>
        public void ShowPopup(Action<IPopup> setupCallback);
        /// <summary>
        /// 显示简单确认弹窗（快捷方法）
        /// </summary>
        public void ShowConfirm(string content, Action onConfirm = null, Action onCancel = null);
        /// <summary>
        /// 显示简单提示弹窗（快捷方法）
        /// </summary>
        public void ShowAlert(string content, Action onConfirm = null);
        #endregion

        #region ItemIcon
        /// <summary>
        /// 获取一个ItemIcon
        /// </summary>
        public void GetItemIcon(Action<IItemIcon> callback);
        #endregion
    }
}