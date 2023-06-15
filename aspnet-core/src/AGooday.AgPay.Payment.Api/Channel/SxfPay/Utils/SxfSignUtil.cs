﻿using AGooday.AgPay.Common.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Payment.Api.Channel.SxfPay.Utils
{
    public class SxfSignUtil
    {
        public static string Sign(JObject reqParams, string privateKey)
        {
            var signString = ConvertSignStringIncludeEmpty(reqParams);
            var sign = RsaUtil.Sign(signString, privateKey);
            return sign;
        }

        public static bool Verify(JObject resParams, string publicKey)
        {
            string sign = resParams.GetValue("sign").ToString();
            var signString = ConvertSignStringIncludeEmpty(resParams);
            var flag = RsaUtil.Verify(signString, publicKey, sign);
            return flag;
        }

        /// <summary>
        /// 将JSON中的数据转换成key1=value1&key2=value2的形式，忽略null、空串内容 和 sign字段*
        /// </summary>
        /// <param name="params"></param>
        /// <returns></returns>
        private static string ConvertSignStringIncludeEmpty(JObject jobjParams)
        {
            SortedDictionary<string, string> keyValuePairs = JsonConvert.DeserializeObject<SortedDictionary<string, string>>(jobjParams.ToString());

            //所有参数进行排序，拼接为 key=value&形式
            var keyvalues = keyValuePairs.Where(w => !w.Key.Equals("sign") && !string.IsNullOrEmpty(w.Value))
                .OrderBy(o => o.Key)
                .Select(s => $"{s.Key}={s.Value}");
            return string.Join("&", keyvalues);
        }
    }
}
