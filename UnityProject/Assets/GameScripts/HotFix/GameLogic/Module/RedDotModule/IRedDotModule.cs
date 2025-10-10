using System;
namespace GameLogic
{
    public interface IRedDotModule
    {
        //设置红点数据
        void SetDot(string word, bool exists);
        //获取节点子节点数量
        int GetNodeChildCount(string word);
        //是否存在节点
        bool IsExistNode(string word);
        //获取叶子节点数
        int GetNodeLeafCount(string word);
    }
}