using System;
namespace GameLogic
{
    public interface IRedDotModule
    {
        //设置红点数据
        void SetDot(string word, bool exists);

        //查询红点数据是否存在
        bool ContainWord(string word);

        //获取节点子节点数量
        int GetNodeChildCount(string word);
    }
}