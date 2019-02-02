using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSO2_ComboLauncher
{
    static class LocalizedStrings
    {
        static Dictionary<KeyValuePair<string, string>, string> lStrings = new Dictionary<KeyValuePair<string, string>, string>();

        public static void InitLocalizedStrings()
        {
            // english
            lStrings.Add(new KeyValuePair<string, string>("START_AS_HOST", "english"), "Start as host");
            lStrings.Add(new KeyValuePair<string, string>("SERVER_SETTING", "english"), "Server Setting");
            lStrings.Add(new KeyValuePair<string, string>("SERVER_ADDRESS", "english"), "Server Address: ");
            lStrings.Add(new KeyValuePair<string, string>("MASTER_PORT", "english"), "Master Port: ");
            lStrings.Add(new KeyValuePair<string, string>("HOLEPUNCH_PORT", "english"), "Holepunch Port: ");
            lStrings.Add(new KeyValuePair<string, string>("LANGUAGE", "english"), "Language: ");
            lStrings.Add(new KeyValuePair<string, string>("LOG", "english"), "Log");
            // schinese
            lStrings.Add(new KeyValuePair<string, string>("START_AS_HOST", "schinese"), "以主机模式启动");
            lStrings.Add(new KeyValuePair<string, string>("SERVER_SETTING", "schinese"), "服务器设置");
            lStrings.Add(new KeyValuePair<string, string>("SERVER_ADDRESS", "schinese"), "服务器地址: ");
            lStrings.Add(new KeyValuePair<string, string>("MASTER_PORT", "schinese"), "服务器端口: ");
            lStrings.Add(new KeyValuePair<string, string>("HOLEPUNCH_PORT", "schinese"), "Holepunch端口: ");
            lStrings.Add(new KeyValuePair<string, string>("LANGUAGE", "schinese"), "语言: ");
            lStrings.Add(new KeyValuePair<string, string>("LOG", "schinese"), "日志");
            // tchinese
            lStrings.Add(new KeyValuePair<string, string>("START_AS_HOST", "tchinese"), "以主機模式啟動");
            lStrings.Add(new KeyValuePair<string, string>("SERVER_SETTING", "tchinese"), "伺服器設定");
            lStrings.Add(new KeyValuePair<string, string>("SERVER_ADDRESS", "tchinese"), "伺服器地址：");
            lStrings.Add(new KeyValuePair<string, string>("MASTER_PORT", "tchinese"), "伺服器埠：");
            lStrings.Add(new KeyValuePair<string, string>("HOLEPUNCH_PORT", "tchinese"), "Holepunch埠：");
            lStrings.Add(new KeyValuePair<string, string>("LANGUAGE", "tchinese"), "語言：");
            lStrings.Add(new KeyValuePair<string, string>("LOG", "tchinese"), "日誌");
        }

        public static string GetLocalizedString(string strCode, string language)
        {
            try
            {
                return lStrings[new KeyValuePair<string, string>(strCode, language)];
            }
            catch
            {
                return strCode;
            }
        }
    }
}
