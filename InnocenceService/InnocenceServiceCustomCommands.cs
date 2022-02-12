namespace InnocenceService
{
    public enum InnocenceServiceCustomCommands
    {
        Recovery = 128,

        /// <summary>
        /// 指示忽略检查条件，开始加密文件。
        /// </summary>
        Encrypt,

        /// <summary>
        /// 指示执行检查，符合条件时将加密文件。
        /// </summary>
        CheckAll,

        /// <summary>
        /// 指示刷新配置。
        /// </summary>
        Refresh,
    }
}
 