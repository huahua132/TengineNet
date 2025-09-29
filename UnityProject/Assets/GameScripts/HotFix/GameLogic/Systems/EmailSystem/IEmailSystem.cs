using System.Collections.Generic;

namespace GameLogic
{
    interface IEmailSystem : ISystem
    {
        hallserver_email.oneEmail GetEmailByGuid(long guid);
        SortedList<(long createTime, long guid), hallserver_email.oneEmail> GetEmailsByType(int emailType);
        int GetUnreadEmailCount(int? emailType = null);
        int GetEmailsWithItemsCount(int? emailType = null);
        int GetTotalEmailCount(int? emailType = null);
        bool HasEmail(long guid);
    }
}