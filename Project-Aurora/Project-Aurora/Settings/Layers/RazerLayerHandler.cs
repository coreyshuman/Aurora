using Aurora.Devices;
using Aurora.Devices.Razer;
using Aurora.EffectsEngine;
using Aurora.Profiles;
using Aurora.Settings.Overrides;
using Aurora.Utils;
using Corale.Colore.Razer.Keyboard;
using Newtonsoft.Json;
using RazerSdkWrapper;
using RazerSdkWrapper.Data;
using RazerSdkWrapper.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Aurora.Settings.Layers
{
    public class RazerLayerHandlerProperties : LayerHandlerProperties<RazerLayerHandlerProperties>
    {
        // Color Enhancing
        public bool? _ColorPostProcessEnabled { get; set; }
        [JsonIgnore]
        public bool ColorPostProcessEnabled => Logic._ColorPostProcessEnabled ?? _ColorPostProcessEnabled ?? false;

        public double? _BrightnessChange { get; set; }
        [JsonIgnore]
        public double BrightnessChange => Logic._BrightnessChange ?? _BrightnessChange ?? 0;

        public double? _SaturationChange { get; set; }
        [JsonIgnore]
        public double SaturationChange => Logic._SaturationChange ?? _SaturationChange ?? 0;

        public double? _HueShift { get; set; }
        [JsonIgnore]
        public double HueShift => Logic._HueShift ?? _HueShift ?? 0;

        public Dictionary<DeviceKeys, DeviceKeys> _KeyCloneMap { get; set; }
        [JsonIgnore]
        public Dictionary<DeviceKeys, DeviceKeys> KeyCloneMap => Logic._KeyCloneMap ?? _KeyCloneMap ?? new Dictionary<DeviceKeys, DeviceKeys>();

        public RazerLayerHandlerProperties() : base() { }

        public RazerLayerHandlerProperties(bool arg = false) : base(arg) { }

        public override void Default()
        {
            base.Default();

            _ColorPostProcessEnabled = false;
            _BrightnessChange = 0;
            _SaturationChange = 0;
            _HueShift = 0;
            _KeyCloneMap = new Dictionary<DeviceKeys, DeviceKeys>();
        }
    }

    [LogicOverrideIgnoreProperty("_PrimaryColor")]
    [LogicOverrideIgnoreProperty("_Sequence")]
    [LayerHandlerMeta(Name = "Razer Chroma", IsDefault = true)]
    public class RazerLayerHandler : LayerHandler<RazerLayerHandlerProperties>
    {
        private Color[] _keyboardColors;
        private Color[] _mousepadColors;
        private Color _mouseColor;
        private string _currentAppExecutable;
        private int _currentAppPid;
        private bool _isDumping;

        public RazerLayerHandler()
        {
            _keyboardColors = new Color[22 * 6];
            _mousepadColors = new Color[16];

            if (Global.razerSdkManager != null)
            {
                Global.razerSdkManager.DataUpdated += OnDataUpdated;

                var appList = Global.razerSdkManager.GetDataProvider<RzAppListDataProvider>();
                appList.Update();
                _currentAppExecutable = appList.CurrentAppExecutable;
                _currentAppPid = appList.CurrentAppPid;
            }
        }

        protected override UserControl CreateControl()
        {
            return new Control_RazerLayer(this);
        }

        private void OnDataUpdated(object s, EventArgs e)
        {
            if (!(s is AbstractDataProvider provider))
                return;

            provider.Update();

            if (_isDumping)
                DumpData(provider);

            if (provider is RzKeyboardDataProvider keyboard)
            {
                byte[] keyboardDataRaw = keyboard.Read();
                for (var i = 0; i < keyboard.Grids[0].Height * keyboard.Grids[0].Width; i++) {
                    int staticIdx = 4 + 76 + keyboard.LatestSnapshot * 2968;
                    int effect = keyboardDataRaw[12 + keyboard.LatestSnapshot * 2968];
                    int zoneidx = 4 + 76 + 528 + keyboard.LatestSnapshot * 2968 + 4;

                    //                          static data      xor  grid data
                    int red = (keyboardDataRaw[staticIdx] ^ keyboardDataRaw[zoneidx + 528 + i * 4]);
                    int green = (keyboardDataRaw[staticIdx + 1] ^ keyboardDataRaw[zoneidx + 528 + 1 + i * 4]);
                    int blue = (keyboardDataRaw[staticIdx + 2] ^ keyboardDataRaw[zoneidx + 528 + 2 + i * 4]);
                    int alpha = (keyboardDataRaw[staticIdx + 3] ^ keyboardDataRaw[zoneidx + 528 + 3 + i * 4]);
                    if(red == 0 && green == 0 && blue == 0 && alpha == 0)
                    {
                        red = (keyboardDataRaw[staticIdx] ^ keyboardDataRaw[zoneidx + i * 4]);
                        green = (keyboardDataRaw[staticIdx + 1] ^ keyboardDataRaw[zoneidx + 1 + i * 4]);
                        blue = (keyboardDataRaw[staticIdx + 2] ^ keyboardDataRaw[zoneidx + 2 + i * 4]);
                        alpha = (keyboardDataRaw[staticIdx + 3] ^ keyboardDataRaw[zoneidx + 3 + i * 4]);
                    }
                    _keyboardColors[i] = Color.FromArgb(255, red, green, blue);
                    //_keyboardColors[i] = keyboard.GetZoneColor(i);
                }
            }
            else if (provider is RzMouseDataProvider mouse)
            {
                byte[] mouseDataRaw = mouse.Read();
                int snapIdx = mouseDataRaw[0] - 1;
                if (snapIdx < 0) snapIdx = 9;
                int staticIdx = 4 + 160 + 252 + 16 + snapIdx * 448;
                int zoneidx = 4 + 160 + snapIdx * 448;
                //                          static data      xor  grid data
                byte red = (byte)(mouseDataRaw[staticIdx] ^ mouseDataRaw[zoneidx + 55 * 4]);
                byte green = (byte)(mouseDataRaw[staticIdx + 1] ^ mouseDataRaw[zoneidx + 1 + 55 * 4]);
                byte blue = (byte)(mouseDataRaw[staticIdx + 2] ^ mouseDataRaw[zoneidx + 2 + 55 * 4]);
                _mouseColor = Color.FromArgb(255, red, green, blue);
                //_mouseColor = mouse.GetZoneColor(55);
            }
            else if (provider is RzMousepadDataProvider mousePad)
            {
                for (var i = 0; i < mousePad.Grids[0].Height * mousePad.Grids[0].Width; i++)
                {
                    byte[] mousePadDataRaw = mousePad.Read();
                    int snapIdx = mousePadDataRaw[0] - 1;
                    if (snapIdx < 0) snapIdx = 9;
                    int staticIdx = 4 + 12 + 32 + snapIdx * 192;
                    int zoneidx = 4 + 12 + 32 + 4 + snapIdx * 192;
                    //                          static data      xor  grid data
                    byte red = (byte)(mousePadDataRaw[staticIdx] ^ mousePadDataRaw[zoneidx + i * 4]);
                    byte green = (byte)(mousePadDataRaw[staticIdx + 1] ^ mousePadDataRaw[zoneidx + 1 + i * 4]);
                    byte blue = (byte)(mousePadDataRaw[staticIdx + 2] ^ mousePadDataRaw[zoneidx + 2 + i * 4]);
                    _mousepadColors[i] = Color.FromArgb(255, red, green, blue);
                }
            }
            else if (provider is RzAppListDataProvider appList)
            {
                _currentAppExecutable = appList.CurrentAppExecutable;
                _currentAppPid = appList.CurrentAppPid;
            }
        }

        public bool StartDumpingData()
        {
            var root = Global.LogsDirectory;
            if (!Directory.Exists(root))
                return false;

            var path = $@"{root}\RazerLayer";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var file in Directory.EnumerateFiles(path, "*.bin", SearchOption.TopDirectoryOnly))
                File.Delete(file);

            Global.logger.Info("RazerLayerHandler started dumping data");
            _isDumping = true;
            return true;
        }

        public void StopDumpingData()
        {
            Global.logger.Info("RazerLayerHandler stopped dumping data");
            _isDumping = false;
        }

        public void DumpData(AbstractDataProvider provider)
        {
            var path = Path.Combine(Global.LogsDirectory, "RazerLayer");
            var filename = $"{provider.GetType().Name}_{Environment.TickCount}.bin";
            using (var file = File.Open($@"{path}\{filename}", FileMode.Create)) {
                var data = provider.Read();
                file.Write(data, 0, data.Length);
            }
        }

        public override EffectLayer Render(IGameState gamestate)
        {
            var layer = new EffectLayer();

            if (!IsCurrentAppValid())
                return layer;

            foreach (var key in (DeviceKeys[])Enum.GetValues(typeof(DeviceKeys)))
            {
                if (!TryGetColor(key, out Color color))
                    continue;
                
                layer.Set(key, color);
            }

            if (Properties.KeyCloneMap != null)
                foreach (var target in Properties.KeyCloneMap)
                    if(TryGetColor(target.Value, out var clr))
                        layer.Set(target.Key, clr);

            return layer;
        }

        private bool TryGetColor(DeviceKeys key, out Color color)
        {
            color = Color.Transparent;
            if (RazerLayoutMap.GenericKeyboard.TryGetValue(key, out var position))
                color = _keyboardColors[position[1] + position[0] * 22];
            else if (key >= DeviceKeys.MOUSEPADLIGHT1 && key <= DeviceKeys.MOUSEPADLIGHT15)
                color = _mousepadColors[DeviceKeys.MOUSEPADLIGHT15 - key];
            else if (key == DeviceKeys.Peripheral)
                color = _mouseColor;
            else
                return false;

            if (Properties.ColorPostProcessEnabled)
                color = PostProcessColor(color);

            return true;
        }

        private bool IsCurrentAppValid() 
            => !string.IsNullOrEmpty(_currentAppExecutable)
            && string.Compare(_currentAppExecutable, "Aurora.exe", true) != 0;

        private Color PostProcessColor(Color color)
        {
            if (color.R == 0 && color.G == 0 && color.B == 0)
                return color;

            if (Properties.BrightnessChange != 0)
                color = ColorUtils.ChangeBrightness(color, Properties.BrightnessChange);
            if (Properties.SaturationChange != 0)
                color = ColorUtils.ChangeSaturation(color, Properties.SaturationChange);
            if (Properties.HueShift != 0)
                color = ColorUtils.ChangeHue(color, Properties.HueShift);

            return color;
        }

        public override void Dispose()
        {
            if(Global.razerSdkManager != null)
                Global.razerSdkManager.DataUpdated -= OnDataUpdated;

            base.Dispose();
        }
    }
}
