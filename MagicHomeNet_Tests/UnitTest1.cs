using MagicHomeNet;
using MagicHomeNet.Common;
using System.Drawing;

namespace MagicHomeNet_Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task Discover_ShouldBe_Ok()
        {
            var provider = new MagicHomeNet.MagicHomeProvider();
            var rst = await provider.Discover();
            Assert.IsNotNull(rst);
        }


        [TestMethod]
        public async Task TurnOn_ShouldBe_Ok()
        {
            var provider = new MagicHomeNet.MagicHomeProvider();
            var rst = await provider.Discover();
            var device = rst.devices.FirstOrDefault();

            await device.Connect();
            await device.TurnOn();
        }

        [TestMethod]
        public async Task TurnOff_ShouldBe_Ok()
        {
            var provider = new MagicHomeNet.MagicHomeProvider();
            var rst = await provider.Discover();
            var device = rst.devices.FirstOrDefault();

            await device.Connect();
            await device.TurnOff();
        }

        [TestMethod]
        public async Task GetColor_ShouldBe_Ok()
        {
            var provider = new MagicHomeProvider();
            var rst = await provider.Discover();
            var device = rst.devices.FirstOrDefault() as MagicHomeDevice;

            await device.Connect();
            await device.GetStatus();
        }

        [TestMethod]
        public async Task SetColor_ShouldBe_Ok()
        {
            var provider = new MagicHomeProvider();
            var rst = await provider.Discover();
            var device = rst.devices.FirstOrDefault();

            await device.Connect();
            await device.TurnOn();
            await Task.Delay(5000);
            await device.SetColor(Color.Violet);
            await Task.Delay(5000);
            await device.SetColor(Color.Yellow);
        }

        [TestMethod]
        public async Task SetPreset_ShouldBe_Ok()
        {
            var provider = new MagicHomeProvider();
            var rst = await provider.Discover();
            var device = rst.devices.FirstOrDefault();

            await device.Connect();
            await device.TurnOn();
            await Task.Delay(100);
            await device.SetPresetPattern(PresetPattern.BlueGradualChange, 1.0);
            await Task.Delay(100);
            await device.SetPresetPattern(PresetPattern.RedGradualChange, 1.0);
        }
    }
}