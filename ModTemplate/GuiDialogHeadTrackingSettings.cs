using System;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VSDOF
{
    internal sealed class GuiDialogHeadTrackingSettings : GuiDialog
    {
        private const string DialogKey = "vsdofsettings";
        private const string ConfigFileName = "vsdof.json";

        private const string KeyEnableTracking = "enabletracking";
        private const string KeyEnableRotation = "enablerotation";
        private const string KeyEnableTranslation = "enabletranslation";
        private const string KeyEnableRoll = "enableroll";
        private const string KeyEnableCrouch = "enablecrouch";

        private const string KeyYawGain = "yawg";
        private const string KeyPitchGain = "pitchg";
        private const string KeyRollGain = "rollg";
        private const string KeyTranslationGain = "transg";
        private const string KeyMaxTranslation = "maxtrans";
        private const string KeyTranslationGainX = "transgx";
        private const string KeyTranslationGainY = "transgy";
        private const string KeyTranslationGainZ = "transgz";
        private const string KeyMaxTranslationX = "maxtransx";
        private const string KeyMaxTranslationY = "maxtransy";
        private const string KeyMaxTranslationZ = "maxtransz";

        private const string KeyCrouchThreshold = "crouchthreshold";
        private const string KeyCrouchHysteresis = "crouchhysteresis";
        private const string KeyCrouchMode = "crouchmode";
        private const string KeyCrouchAxis = "crouchaxis";

        private static readonly string[] CrouchModeCodes = { "hold", "toggle" };
        private static readonly string[] CrouchModeNames = { "Hold", "Toggle" };
        private static readonly string[] CrouchAxisCodes = { "X", "Y", "Z" };
        private static readonly string[] CrouchAxisNames = { "X", "Y", "Z" };

        private readonly ICoreClientAPI capi;
        private GuiComposer composer;

        public GuiDialogHeadTrackingSettings(ICoreClientAPI capi) : base(capi)
        {
            this.capi = capi;
        }

        public override string ToggleKeyCombinationCode => null;

        public void SyncValues()
        {
            if (SingleComposer == null)
            {
                ComposeDialog();
            }

            var config = Config;
            SetSwitch(KeyEnableTracking, config.EnableTracking);
            SetSwitch(KeyEnableRotation, config.EnableRotation);
            SetSwitch(KeyEnableTranslation, config.EnableTranslation);
            SetSwitch(KeyEnableRoll, config.EnableRoll);
            SetSwitch(KeyEnableCrouch, config.EnableCrouchToggle);

            SetNumber(KeyYawGain, config.YawGain);
            SetNumber(KeyPitchGain, config.PitchGain);
            SetNumber(KeyRollGain, config.RollGain);
            SetNumber(KeyTranslationGain, config.TranslationGain);
            SetNumber(KeyMaxTranslation, config.MaxTranslation);
            SetNumber(KeyTranslationGainX, config.TranslationGainX);
            SetNumber(KeyTranslationGainY, config.TranslationGainY);
            SetNumber(KeyTranslationGainZ, config.TranslationGainZ);
            SetNumber(KeyMaxTranslationX, config.MaxTranslationX);
            SetNumber(KeyMaxTranslationY, config.MaxTranslationY);
            SetNumber(KeyMaxTranslationZ, config.MaxTranslationZ);

            SetNumber(KeyCrouchThreshold, config.CrouchThreshold);
            SetNumber(KeyCrouchHysteresis, config.CrouchHysteresis);
            SetDropDown(KeyCrouchMode, config.CrouchMode, CrouchModeCodes);
            SetDropDown(KeyCrouchAxis, config.CrouchAxis, CrouchAxisCodes);
        }

        private void ComposeDialog()
        {
            double pad = GuiStyle.ElementToDialogPadding;
            double labelWidth = 160;
            double inputWidth = 90;
            double dropWidth = 120;
            double switchWidth = 30;
            double rowHeight = 28;
            double colGap = 30;
            int rows = 11;
            double startY = pad + 30;

            double colWidth = labelWidth + 10 + dropWidth;
            double dialogWidth = pad + colWidth + colGap + colWidth + pad;
            double dialogHeight = startY + rows * rowHeight + pad;

            ElementBounds dialogBounds = ElementBounds
                .Fixed(0, 0, dialogWidth, dialogHeight)
                .WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(pad);

            composer?.Dispose();
            composer = capi.Gui.CreateCompo(DialogKey, dialogBounds)
                .AddGrayBG(bgBounds)
                .AddDialogTitleBar("VSDOF Head Tracking", OnTitleBarClose);

            CairoFont labelFont = CairoFont.WhiteSmallText();
            CairoFont inputFont = CairoFont.TextInput();

            double leftX = pad;
            double rightX = leftX + colWidth + colGap;
            double leftY = startY;

            AddSwitchRow(leftX, leftY, labelWidth, switchWidth, rowHeight, labelFont,
                "Enable Tracking", KeyEnableTracking, value => UpdateBool(value, (cfg, v) => cfg.EnableTracking = v));
            leftY += rowHeight;
            AddSwitchRow(leftX, leftY, labelWidth, switchWidth, rowHeight, labelFont,
                "Enable Rotation", KeyEnableRotation, value => UpdateBool(value, (cfg, v) => cfg.EnableRotation = v));
            leftY += rowHeight;
            AddSwitchRow(leftX, leftY, labelWidth, switchWidth, rowHeight, labelFont,
                "Enable Translation", KeyEnableTranslation, value => UpdateBool(value, (cfg, v) => cfg.EnableTranslation = v));
            leftY += rowHeight;
            AddSwitchRow(leftX, leftY, labelWidth, switchWidth, rowHeight, labelFont,
                "Enable Roll", KeyEnableRoll, value => UpdateBool(value, (cfg, v) => cfg.EnableRoll = v));
            leftY += rowHeight;
            AddSwitchRow(leftX, leftY, labelWidth, switchWidth, rowHeight, labelFont,
                "Enable Crouch", KeyEnableCrouch, value => UpdateBool(value, (cfg, v) => cfg.EnableCrouchToggle = v));
            leftY += rowHeight;
            AddNumberRow(leftX, leftY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Yaw Gain", KeyYawGain, value => UpdateFloat(value, (cfg, v) => cfg.YawGain = v));
            leftY += rowHeight;
            AddNumberRow(leftX, leftY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Pitch Gain", KeyPitchGain, value => UpdateFloat(value, (cfg, v) => cfg.PitchGain = v));
            leftY += rowHeight;
            AddNumberRow(leftX, leftY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Roll Gain", KeyRollGain, value => UpdateFloat(value, (cfg, v) => cfg.RollGain = v));
            leftY += rowHeight;
            AddNumberRow(leftX, leftY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Translation Gain", KeyTranslationGain, value => UpdateFloat(value, (cfg, v) => cfg.TranslationGain = v));
            leftY += rowHeight;
            AddNumberRow(leftX, leftY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Max Translation", KeyMaxTranslation, value => UpdateFloat(value, (cfg, v) => cfg.MaxTranslation = v));

            double rightY = startY;
            AddNumberRow(rightX, rightY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Translation Gain X", KeyTranslationGainX, value => UpdateFloat(value, (cfg, v) => cfg.TranslationGainX = v));
            rightY += rowHeight;
            AddNumberRow(rightX, rightY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Translation Gain Y", KeyTranslationGainY, value => UpdateFloat(value, (cfg, v) => cfg.TranslationGainY = v));
            rightY += rowHeight;
            AddNumberRow(rightX, rightY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Translation Gain Z", KeyTranslationGainZ, value => UpdateFloat(value, (cfg, v) => cfg.TranslationGainZ = v));
            rightY += rowHeight;
            AddNumberRow(rightX, rightY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Max Translation X", KeyMaxTranslationX, value => UpdateFloat(value, (cfg, v) => cfg.MaxTranslationX = v));
            rightY += rowHeight;
            AddNumberRow(rightX, rightY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Max Translation Y", KeyMaxTranslationY, value => UpdateFloat(value, (cfg, v) => cfg.MaxTranslationY = v));
            rightY += rowHeight;
            AddNumberRow(rightX, rightY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Max Translation Z", KeyMaxTranslationZ, value => UpdateFloat(value, (cfg, v) => cfg.MaxTranslationZ = v));
            rightY += rowHeight;
            AddNumberRow(rightX, rightY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Crouch Threshold", KeyCrouchThreshold, value => UpdateFloat(value, (cfg, v) => cfg.CrouchThreshold = v));
            rightY += rowHeight;
            AddNumberRow(rightX, rightY, labelWidth, inputWidth, rowHeight, labelFont, inputFont,
                "Crouch Hysteresis", KeyCrouchHysteresis, value => UpdateFloat(value, (cfg, v) => cfg.CrouchHysteresis = v));
            rightY += rowHeight;
            AddDropDownRow(rightX, rightY, labelWidth, dropWidth, rowHeight, labelFont,
                "Crouch Mode", KeyCrouchMode, CrouchModeCodes, CrouchModeNames,
                (code, selected) => UpdateSelection(code, selected, (cfg, v) => cfg.CrouchMode = v));
            rightY += rowHeight;
            AddDropDownRow(rightX, rightY, labelWidth, dropWidth, rowHeight, labelFont,
                "Crouch Axis", KeyCrouchAxis, CrouchAxisCodes, CrouchAxisNames,
                (code, selected) => UpdateSelection(code, selected, (cfg, v) => cfg.CrouchAxis = v));

            double buttonWidth = 160;
            double buttonX = (dialogWidth - buttonWidth) / 2;
            double buttonY = startY + 10 * rowHeight + 2;
            composer.AddButton(
                "Reset to defaults",
                OnResetClicked,
                ElementBounds.Fixed(buttonX, buttonY, buttonWidth, rowHeight),
                CairoFont.ButtonText(),
                EnumButtonStyle.Normal,
                "resetdefaults");

            SingleComposer = composer.Compose();
        }

        private void AddSwitchRow(
            double x,
            double y,
            double labelWidth,
            double switchWidth,
            double rowHeight,
            CairoFont labelFont,
            string label,
            string key,
            Action<bool> onToggle)
        {
            composer.AddStaticText(label, labelFont, ElementBounds.Fixed(x, y + 6, labelWidth, rowHeight), null);
            composer.AddSwitch(onToggle, ElementBounds.Fixed(x + labelWidth + 10, y, switchWidth, rowHeight), key, 20, 4);
        }

        private void AddNumberRow(
            double x,
            double y,
            double labelWidth,
            double inputWidth,
            double rowHeight,
            CairoFont labelFont,
            CairoFont inputFont,
            string label,
            string key,
            Action<string> onChanged)
        {
            composer.AddStaticText(label, labelFont, ElementBounds.Fixed(x, y + 6, labelWidth, rowHeight), null);
            composer.AddNumberInput(ElementBounds.Fixed(x + labelWidth + 10, y, inputWidth, rowHeight), onChanged, inputFont, key);
        }

        private void AddDropDownRow(
            double x,
            double y,
            double labelWidth,
            double dropWidth,
            double rowHeight,
            CairoFont labelFont,
            string label,
            string key,
            string[] codes,
            string[] names,
            SelectionChangedDelegate onChanged)
        {
            composer.AddStaticText(label, labelFont, ElementBounds.Fixed(x, y + 6, labelWidth, rowHeight), null);
            composer.AddDropDown(codes, names, 0, onChanged, ElementBounds.Fixed(x + labelWidth + 10, y, dropWidth, rowHeight), key);
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }

        private bool OnResetClicked()
        {
            VsdofModSystem.Config = new HeadTrackingConfig();
            StoreConfig();
            SyncValues();
            return true;
        }

        private HeadTrackingConfig Config
        {
            get
            {
                if (VsdofModSystem.Config == null)
                {
                    VsdofModSystem.Config = new HeadTrackingConfig();
                }

                return VsdofModSystem.Config;
            }
        }

        private void StoreConfig()
        {
            capi.StoreModConfig(Config, ConfigFileName);
        }

        private void UpdateBool(bool value, Action<HeadTrackingConfig, bool> apply)
        {
            apply(Config, value);
            StoreConfig();
        }

        private void UpdateSelection(string code, bool selected, Action<HeadTrackingConfig, string> apply)
        {
            if (!selected || string.IsNullOrEmpty(code))
            {
                return;
            }

            apply(Config, code);
            StoreConfig();
        }

        private void UpdateFloat(string value, Action<HeadTrackingConfig, float> apply)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return;
            }

            apply(Config, parsed);
            StoreConfig();
        }

        private void SetSwitch(string key, bool value)
        {
            var element = composer?.GetSwitch(key);
            element?.SetValue(value);
        }

        private void SetNumber(string key, float value)
        {
            var element = composer?.GetNumberInput(key);
            if (element == null)
            {
                return;
            }

            var elementType = element.GetType();
            var setFloat = elementType.GetMethod("SetValue", new[] { typeof(float) });
            if (setFloat != null)
            {
                setFloat.Invoke(element, new object[] { value });
                return;
            }

            var setDouble = elementType.GetMethod("SetValue", new[] { typeof(double) });
            if (setDouble != null)
            {
                setDouble.Invoke(element, new object[] { (double)value });
                return;
            }

            var setString = elementType.GetMethod("SetValue", new[] { typeof(string) });
            if (setString != null)
            {
                setString.Invoke(element, new object[] { value.ToString(CultureInfo.InvariantCulture) });
                return;
            }

            var textProp = elementType.GetProperty("Text");
            if (textProp != null && textProp.CanWrite && textProp.PropertyType == typeof(string))
            {
                textProp.SetValue(element, value.ToString(CultureInfo.InvariantCulture));
                return;
            }

            var valueProp = elementType.GetProperty("Value");
            if (valueProp != null && valueProp.CanWrite)
            {
                if (valueProp.PropertyType == typeof(float))
                {
                    valueProp.SetValue(element, value);
                }
                else if (valueProp.PropertyType == typeof(double))
                {
                    valueProp.SetValue(element, (double)value);
                }
            }
        }

        private void SetDropDown(string key, string value, string[] codes)
        {
            var element = composer?.GetDropDown(key);
            if (element == null)
            {
                return;
            }

            int index = Array.FindIndex(codes, code => string.Equals(code, value, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                index = 0;
            }

            element.SetSelectedIndex(index);
        }
    }
}
