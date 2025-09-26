namespace GameLogic
{
    public enum SystemPriority
    {
        Login = 10000,                 //登录
        HallMain = 9999,               //大厅主系统
        Base = 9000,                   //基础数据相关
        Default = 1000,                //默认优先级
    }
}