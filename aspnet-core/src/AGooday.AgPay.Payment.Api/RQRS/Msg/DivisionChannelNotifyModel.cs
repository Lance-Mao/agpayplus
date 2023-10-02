﻿using Microsoft.AspNetCore.Mvc;

namespace AGooday.AgPay.Payment.Api.RQRS.Msg
{
    /// <summary>
    /// 封装响应结果的数据
    /// 直接写：  Dictionary<ResponseEntity, Dictionary<Long, ChannelRetMsg>> 太过复杂！
    /// </summary>
    public class DivisionChannelNotifyModel
    {
        /// <summary>
        /// 响应接口返回的数据
        /// </summary>
        public ActionResult ApiRes { get; set; }

        /** 每一条记录的更新状态 <ID, 结果> **/
        public Dictionary<long, ChannelRetMsg> RecordResultMap { get; set; }
    }
}
