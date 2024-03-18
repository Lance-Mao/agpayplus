using AGooday.AgPay.Common.Utils;

namespace AGooday.AgPay.Common.UnitTests
{
    [TestClass]
    public class AgPayUtilTest
    {
        /// <summary>
        /// https://tool.lmeee.com/jiami/aes
        /// AES����ģʽ��ECB ��䣺pkcs7padding ��Կ���ȣ�192λ ��Կ��4ChT08phkz59hquD795X7w== �����hex
        /// </summary>
        [TestMethod]
        public void AgPayAesTest()
        {
            var data = "65d86708e4b0c884420d9b9a";
            var cipherText = AgPayUtil.AesEncode(data);
            var plainText = AgPayUtil.AesDecode(cipherText);
            var _cipherText = "4608adcbef5a3e18f60a4c1a92bef61db4fff4d1e1b250b8775b1c7317605836";
            var _plainText = AgPayUtil.AesDecode(_cipherText);

            Assert.AreEqual(cipherText, _cipherText, true);
            Assert.AreEqual(data, plainText);
            Assert.AreEqual(data, _plainText);
        }
    }
}