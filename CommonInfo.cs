using System;
using System.Collections.Generic;
using System.Text;

namespace Telegram_Bot
{
    public class CommonInfo
    {
        public static string MainFolderPath => @"C:\ProgramData\Telegram bot";

        public static string JsonDatabasePath => @"C:\ProgramData\Telegram bot\JSON database.json";

        public static string CategoryRefreshAmount { get; set; }
        public static string ItemRefreshAmount { get; set; }
        public static string NewAmount { get; set; }
    }
}
