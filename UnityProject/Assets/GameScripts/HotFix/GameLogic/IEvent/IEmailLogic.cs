using TEngine;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IEmailLogic
    {
        //接收到一条邮件数据
        void RecvOneEmailNotice();

        //标记邮件已读
        void EmailReadFlag(long guid);

        //标记邮件已领奖
        void EmailRewardFlag(long guid);
    }
}