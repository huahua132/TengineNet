using TEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GameLogic
{
    [System((int)SystemPriority.Base)]
    public class EmailSystem : IEmailSystem
    {
        // 按邮件类型分类存储，每个类型内按创建时间和guid排序
        private Dictionary<int, SortedList<(long createTime, long guid), hallserver_email.oneEmail>> _emailsByType = new();
        
        // 快速查找邮件的映射表
        private Dictionary<long, hallserver_email.oneEmail> _emailMap = new();

        public void OnInit()
        {
            GameModule.NetHall.RegisterMessageListener(hallserver_email.MessageId.AllEmailNotice, OnRecvAllEmainNotice);
            GameModule.NetHall.RegisterMessageListener(hallserver_email.MessageId.OneEmailNotice, OnRecvOneEmailNotice);
            GameModule.NetHall.RegisterMessageListener(hallserver_email.MessageId.DelEmailNotice, OnRecvDelEmailNotice);
        }

        public void OnStart()
        {
        }

        public void OnDestroy()
        {
            GameModule.NetHall.UnregisterMessageListener(hallserver_email.MessageId.AllEmailNotice, OnRecvAllEmainNotice);
            GameModule.NetHall.UnregisterMessageListener(hallserver_email.MessageId.OneEmailNotice, OnRecvOneEmailNotice);
            GameModule.NetHall.UnregisterMessageListener(hallserver_email.MessageId.DelEmailNotice, OnRecvDelEmailNotice);
        }

        private void OnRecvAllEmainNotice(INetResponse netPack)
        {
            hallserver_email.AllEmailNotice allEmail = netPack.GetResponse<hallserver_email.AllEmailNotice>();
            
            // 清空现有数据
            _emailsByType.Clear();
            _emailMap.Clear();
            
            foreach (var email in allEmail.email_list)
            {
                AddEmailToSystem(email);
            }
        }

        private void OnRecvOneEmailNotice(INetResponse netPack)
        {
            var emailNotice = netPack.GetResponse<hallserver_email.OneEmailNotice>();
            AddEmailToSystem(emailNotice.email);
        }

        private void OnRecvDelEmailNotice(INetResponse netPack)
        {
            var delEmailNotice = netPack.GetResponse<hallserver_email.DelEmailNotice>();
            foreach (var guid in delEmailNotice.guid_list)
            {
                RemoveEmailFromSystem(guid);
            }
        }

        /// <summary>
        /// 添加邮件到系统
        /// </summary>
        private void AddEmailToSystem(hallserver_email.oneEmail email)
        {
            if (email == null) return;
            // 更新快速查找映射
            _emailMap[email.guid] = email;

            // 按类型分类存储
            if (!_emailsByType.ContainsKey(email.email_type))
            {
                _emailsByType[email.email_type] = new SortedList<(long, long), hallserver_email.oneEmail>();
            }

            var key = (email.create_time, email.guid);
            _emailsByType[email.email_type][key] = email;
        }

        /// <summary>
        /// 从系统中移除邮件
        /// </summary>
        private void RemoveEmailFromSystem(long guid)
        {
            if (!_emailMap.ContainsKey(guid)) return;

            var email = _emailMap[guid];
            _emailMap.Remove(guid);

            // 从分类存储中移除
            if (_emailsByType.ContainsKey(email.email_type))
            {
                var key = (email.create_time, email.guid);
                _emailsByType[email.email_type].Remove(key);

                // 如果该类型下没有邮件了，移除该类型
                if (_emailsByType[email.email_type].Count == 0)
                {
                    _emailsByType.Remove(email.email_type);
                }
            }
        }

        #region 公共接口方法

        /// <summary>
        /// 根据guid获取邮件
        /// </summary>
        public hallserver_email.oneEmail GetEmailByGuid(long guid)
        {
            return _emailMap.TryGetValue(guid, out var email) ? email : null;
        }

        /// <summary>
        /// 获取指定类型的所有邮件（已排序）
        /// </summary>
        public SortedList<(long createTime, long guid), hallserver_email.oneEmail> GetEmailsByType(int emailType)
        {
            if (!_emailsByType.TryGetValue(emailType, out var list))
                return new SortedList<(long createTime, long guid), hallserver_email.oneEmail>();

            return list;
        }

        /// <summary>
        /// 获取未读邮件数量
        /// </summary>
        public int GetUnreadEmailCount(int? emailType = null)
        {
            if (emailType.HasValue)
            {
                return GetEmailsByType(emailType.Value).Count(e => e.Value.read_flag == 0);
            }
            
            return _emailMap.Values.Count(e => e.read_flag == 0);
        }

        /// <summary>
        /// 获取有附件的邮件数量
        /// </summary>
        public int GetEmailsWithItemsCount(int? emailType = null)
        {
            if (emailType.HasValue)
            {
                return GetEmailsByType(emailType.Value).Count(e => e.Value.item_flag == 1 && e.Value.item_list.Count > 0);
            }
            
            return _emailMap.Values.Count(e => e.item_flag == 1 && e.item_list.Count > 0);
        }

        /// <summary>
        /// 获取邮件总数
        /// </summary>
        public int GetTotalEmailCount(int? emailType = null)
        {
            if (emailType.HasValue)
            {
                return _emailsByType.ContainsKey(emailType.Value) ? _emailsByType[emailType.Value].Count : 0;
            }
            
            return _emailMap.Count;
        }

        /// <summary>
        /// 检查邮件是否存在
        /// </summary>
        public bool HasEmail(long guid)
        {
            return _emailMap.ContainsKey(guid);
        }

        #endregion
    }
}

