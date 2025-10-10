/*
 * Description:             Trie.cs
 * Author:                  TONYTANG
 * Create Date:             2022/08/12
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 前缀树
    /// </summary>
    public class Trie
    {
        /// <summary>
        /// 单词分隔符
        /// </summary>
        public char Separator
        {
            get;
            private set;
        }

        /// <summary>
        /// 单词数量
        /// </summary>
        public int WorldCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 树深度
        /// </summary>
        public int TrieDeepth
        {
            get;
            private set;
        }

        /// <summary>
        /// 根节点
        /// </summary>
        public TrieNode RootNode
        {
            get;
            private set;
        }

        /// <summary>
        /// 单词列表(用于缓存分割结果，优化单个单词判定时重复分割问题)
        /// </summary>
        private List<string> mWordList;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="separator"></param>
        public Trie(char separator = '|')
        {
            if (char.IsWhiteSpace(separator))
            {
                throw new ArgumentException("分隔符不能是空白字符");
            }
            
            Separator = separator;
            WorldCount = 0;
            TrieDeepth = 0;
            RootNode = MemoryPool.Acquire<TrieNode>();
            RootNode.Init("Root", null, this, 0, false);
            mWordList = new List<string>();
        }

        /// <summary>
        /// 添加单词
        /// </summary>
        /// <param name="word"></param>
        public void AddWord(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogError("不允许添加空单词!");
                return;
            }
            
            mWordList.Clear();
            var words = word.Split(Separator);
            mWordList.AddRange(words);
            
            var wasWordAlreadyExist = ContainWord(word);
            var length = mWordList.Count;
            var node = RootNode;
            
            for (int i = 0; i < length; i++)
            {
                var spliteWord = mWordList[i];
                var isLast = i == (length - 1);
                
                if (!node.ContainWord(spliteWord))
                {
                    node = node.AddChildNode(spliteWord, isLast);
                }
                else
                {
                    node = node.GetChildNode(spliteWord);
                    if (isLast)
                    {
                        node.IsTail = true; // 确保标记为单词结尾
                    }
                }
            }
            
            // 只有新增单词时才计数
            if (!wasWordAlreadyExist)
            {
                WorldCount++;
                TrieDeepth = Math.Max(TrieDeepth, length);
            }
        }

        /// <summary>
        /// 移除指定单词
        /// Note:
        /// 仅当指定单词存在时才能移除成功
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public bool RemoveWord(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogError($"不允许移除空单词!");
                return false;
            }
            
            var wordNode = GetWordNode(word);
            if (wordNode == null || !wordNode.IsTail)
            {
                Debug.LogError($"找不到单词:{word}的节点信息，移除单词失败!");
                return false;
            }
            
            if (wordNode.IsRoot)
            {
                Debug.LogError($"不允许删除根节点!");
                return false;
            }
            
            // 标记为非单词节点
            wordNode.IsTail = false;
            
            // 只有当节点没有子节点且不是根节点时才删除
            var currentNode = wordNode;
            while (!currentNode.IsRoot && 
                   !currentNode.IsTail && 
                   currentNode.ChildCount == 0)
            {
                var parent = currentNode.Parent;
                parent.RemoveChildNode(currentNode);
                currentNode = parent;
            }
            
            WorldCount--;
            return true;
        }

        /// <summary>
        /// 获取指定字符串的单词节点
        /// Note:
        /// 只有满足每一层且最后一层是单词的节点才算有效单词节点
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public TrieNode GetWordNode(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogError($"无法获取空单词的单次节点!");
                return null;
            }
            
            var wordArray = word.Split(Separator);
            var node = RootNode;
            foreach (var spliteWord in wordArray)
            {
                var childNode = node.GetChildNode(spliteWord);
                if (childNode != null)
                {
                    node = childNode;
                }
                else
                {
                    return null;
                }
            }
            
            if (node == null || !node.IsTail)
            {
                return null;
            }
            
            return node;
        }

        /// <summary>
        /// 有按指定单词开头的词语
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public bool StartWith(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }
            
            mWordList.Clear();
            var wordArray = word.Split(Separator);
            mWordList.AddRange(wordArray);
            return FindWord(RootNode, new List<string>(mWordList)); // 使用副本避免修改原列表
        }

        /// <summary>
        /// 查找单词
        /// </summary>
        /// <param name="trieNode"></param>
        /// <param name="wordList"></param>
        /// <returns></returns>
        private bool FindWord(TrieNode trieNode, List<string> wordList)
        {
            if (wordList.Count == 0)
            {
                return true;
            }
            
            var firstWord = wordList[0];
            if (!trieNode.ContainWord(firstWord))
            {
                return false;
            }
            
            var childNode = trieNode.GetChildNode(firstWord);
            wordList.RemoveAt(0);
            return FindWord(childNode, wordList);
        }

        /// <summary>
        /// 单词是否存在
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public bool ContainWord(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }
            
            mWordList.Clear();
            var wordArray = word.Split(Separator);
            mWordList.AddRange(wordArray);
            return MatchWord(RootNode, new List<string>(mWordList)); // 使用副本避免修改原列表
        }

        /// <summary>
        /// 匹配单词(单词必须完美匹配)
        /// </summary>
        /// <param name="trieNode"></param>
        /// <param name="wordList"></param>
        /// <returns></returns>
        private bool MatchWord(TrieNode trieNode, List<string> wordList)
        {
            if (wordList.Count == 0)
            {
                return trieNode.IsTail;
            }
            
            var firstWord = wordList[0];
            if (!trieNode.ContainWord(firstWord))
            {
                return false;
            }
            
            var childNode = trieNode.GetChildNode(firstWord);
            wordList.RemoveAt(0);
            return MatchWord(childNode, wordList);
        }

        /// <summary>
        /// 获取所有单词列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetWordList()
        {
            return GetNodeWorldList(RootNode, string.Empty);
        }

        /// <summary>
        /// 获取节点单词列表
        /// </summary>
        /// <param name="trieNode"></param>
        /// <param name="preFix"></param>
        /// <returns></returns>
        private List<string> GetNodeWorldList(TrieNode trieNode, string preFix)
        {
            var wordList = new List<string>();
            foreach (var childNodeKey in trieNode.ChildNodesMap.Keys)
            {
                var childNode = trieNode.ChildNodesMap[childNodeKey];
                string word;
                if (trieNode.IsRoot)
                {
                    word = $"{preFix}{childNodeKey}";
                }
                else
                {
                    word = $"{preFix}{Separator}{childNodeKey}";
                }

                if (childNode.IsTail)
                {
                    wordList.Add(word);
                }

                if (childNode.ChildNodesMap.Count > 0)
                {
                    var childNodeWorldList = GetNodeWorldList(childNode, word);
                    wordList.AddRange(childNodeWorldList);
                }
            }
            return wordList;
        }
        
        /// <summary>
        /// 查询是否存在节点
        /// </summary>
        public bool IsExistNode(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogError($"无法获取空单词的单次节点!");
                return false;
            }
            
            var wordArray = word.Split(Separator);
            var node = RootNode;
            foreach (var spliteWord in wordArray)
            {
                var childNode = node.GetChildNode(spliteWord);
                if (childNode != null)
                {
                    node = childNode;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取节点直接子节点数
        /// </summary>
        public int GetNodeChildCount(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogError($"无法获取空单词的单次节点!");
                return 0;
            }
            
            var wordArray = word.Split(Separator);
            var node = RootNode;
            foreach (var spliteWord in wordArray)
            {
                var childNode = node.GetChildNode(spliteWord);
                if (childNode != null)
                {
                    node = childNode;
                }
                else
                {
                    return 0;
                }
            }
            
            return node.ChildCount;
        }

        /// <summary>
        /// 获取节点下所有叶子节点数（即单词结尾节点数）
        /// </summary>
        /// <param name="word">目标节点路径</param>
        /// <returns>叶子节点数量</returns>
        public int GetNodeLeafCount(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogError($"无法获取空单词的节点信息!");
                return 0;
            }
            
            var wordArray = word.Split(Separator);
            var node = RootNode;
            foreach (var spliteWord in wordArray)
            {
                var childNode = node.GetChildNode(spliteWord);
                if (childNode != null)
                {
                    node = childNode;
                }
                else
                {
                    return 0;
                }
            }
            
            return CountLeafNodes(node);
        }

        /// <summary>
        /// 递归统计节点下的叶子节点数量
        /// </summary>
        /// <param name="node">起始节点</param>
        /// <returns>叶子节点数量</returns>
        private int CountLeafNodes(TrieNode node)
        {
            if (node == null)
            {
                return 0;
            }
            
            int count = 0;
            
            // 如果当前节点是单词结尾，计数+1
            if (node.IsTail)
            {
                count++;
            }
            
            // 递归统计所有子节点
            foreach (var childNode in node.ChildNodesMap.Values)
            {
                count += CountLeafNodes(childNode);
            }
            
            return count;
        }

        /// <summary>
        /// 获取整棵树的叶子节点数（应该等于WorldCount）
        /// </summary>
        /// <returns>叶子节点总数</returns>
        public int GetTotalLeafCount()
        {
            return CountLeafNodes(RootNode);
        }

        /// <summary>
        /// 清空Trie树
        /// </summary>
        public void Clear()
        {
            // 递归释放所有子节点
            foreach (var child in RootNode.ChildNodesMap.Values.ToList())
            {
                RootNode.RemoveChildNode(child);
            }

            WorldCount = 0;
            TrieDeepth = 0;
        }

        /// <summary>
        /// 打印树形节点
        /// </summary>
        public void PrintTreeNodes()
        {
            PrintNodes(RootNode, 1);
        }

        /// <summary>
        /// 打印节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="depth"></param>
        private void PrintNodes(TrieNode node, int depth = 1)
        {
            var count = 1;
            foreach (var childeNode in node.ChildNodesMap)
            {
                Console.Write($"{childeNode.Key}({depth}-{count})");
                count++;
            }
            Console.WriteLine();
            foreach (var childeNode in node.ChildNodesMap)
            {
                PrintNodes(childeNode.Value, depth + 1);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Clear();
            if (RootNode != null)
            {
                MemoryPool.Release(RootNode);
                RootNode = null;
            }
        }
    }
}

