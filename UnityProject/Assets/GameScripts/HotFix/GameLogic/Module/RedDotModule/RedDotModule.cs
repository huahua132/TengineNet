using TEngine;
using System;
using UnityEngine.UIElements;
using System.Linq;

namespace GameLogic
{
    public class RedDotModule : Module, IRedDotModule
    {
        private Trie _rootTrie;
        public override void OnInit()
        {
            _rootTrie = new Trie();
        }

        public override void Shutdown()
        {
            _rootTrie = null;
        }

        //触发红点变更事件 假如有如下红点节点 email|sys|cell_1 会触发email|sys|cell_1 email|sys email 3次事件
        private void pushDotChangeEvent(string word)
        {
            string[] words = word.Split('|');
            string eventWord = words[0];
            GameEvent.Send(eventWord);
            Log.Info($"pushDotChangeEvent >>> {eventWord}");
            for (int i = 1; i < words.Length; i++)
            {
                eventWord = eventWord + '|' + words[i];
                GameEvent.Send(eventWord);
                Log.Info($"pushDotChangeEvent >>> {eventWord}");
            }
        }

        //设置红点数据
        public void SetDot(string word, bool exists)
        {
            var isHave = _rootTrie.ContainWord(word);
            if (exists)
            {
                if (isHave) return;
                _rootTrie.AddWord(word);
            }
            else
            {
                if (!isHave) return;
                _rootTrie.RemoveWord(word);
            }
            
            pushDotChangeEvent(word);
        }

        //查询红点数据是否存在
        public bool ContainWord(string word)
        {
            return _rootTrie.ContainWord(word);
        }

        //获取节点子节点数量
        public int GetNodeChildCount(string word)
        {
            return _rootTrie.GetNodeChildCount(word);
        }
    }
}