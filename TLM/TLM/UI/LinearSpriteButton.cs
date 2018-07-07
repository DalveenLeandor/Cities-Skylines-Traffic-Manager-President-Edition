using ColossalFramework.Math;
using ColossalFramework.UI;
using CSUtil.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrafficManager.State;
using TrafficManager.Util;
using UnityEngine;

namespace TrafficManager.UI {
	public abstract class LinearSpriteButton : UIButton {
		[Flags]
		public enum ButtonMouseStates {
			None = 0,
			Base = 1,
			Hovered = 1 << 1,
			Pressed = 1 << 2,
			Focused = 1 << 3
		}

		[Flags]
		public enum ButtonFunctionStates {
			None,
			Default = 1,
			Active = 1 << 1,
			Disabled = 1 << 2
		}

		public const string MENU_BUTTON_BACKGROUND = "Bg";
		public const string MENU_BUTTON_FOREGROUND = "Fg";

		public const string MENU_BUTTON_BASE = "Base";
		public const string MENU_BUTTON_HOVERED = "Hovered";
		public const string MENU_BUTTON_PRESSED = "Pressed";
		public const string MENU_BUTTON_FOCUSED = "Focused";

		public const string MENU_BUTTON_DEFAULT = "Default";
		public const string MENU_BUTTON_ACTIVE = "Active";
		public const string MENU_BUTTON_DISABLED = "Disabled";

		private static string GetButtonTextureId(string prefix, ButtonMouseStates mouseState, ButtonFunctionStates functionState) {
			string ret = prefix;

			if (functionState == ButtonFunctionStates.Disabled) {
				mouseState = ButtonMouseStates.Base;
			}

			switch (mouseState) {
				case ButtonMouseStates.Base:
					ret += MENU_BUTTON_BASE;
					break;
				case ButtonMouseStates.Hovered:
					ret += MENU_BUTTON_HOVERED;
					break;
				case ButtonMouseStates.Pressed:
					ret += MENU_BUTTON_PRESSED;
					break;
				case ButtonMouseStates.Focused:
					ret += MENU_BUTTON_FOCUSED;
					break;
			}

			switch (functionState) {
				case ButtonFunctionStates.Default:
					ret += MENU_BUTTON_DEFAULT;
					break;
				case ButtonFunctionStates.Active:
					ret += MENU_BUTTON_ACTIVE;
					break;
				case ButtonFunctionStates.Disabled:
					ret += MENU_BUTTON_DISABLED;
					break;
			}

			return ret;
		}

		protected static string GetButtonBackgroundTextureId(string prefix, ButtonMouseStates mouseState, ButtonFunctionStates functionState) {
			return GetButtonTextureId(prefix + MENU_BUTTON_BACKGROUND, mouseState, functionState);
		}

		protected static string GetButtonForegroundTextureId(string prefix, string function, ButtonMouseStates mouseState, ButtonFunctionStates functionState) {
			return GetButtonTextureId(prefix + MENU_BUTTON_FOREGROUND + function, mouseState, functionState);
		}

		public abstract ButtonMouseStates SupportedBgMouseStatesMask { get; }
		public abstract ButtonFunctionStates SupportedBgFunctionStatesMask { get; }
		public abstract ButtonMouseStates SupportedFgMouseStatesMask { get; }
		public abstract ButtonFunctionStates SupportedFgFunctionStatesMask { get; }

		public abstract string ButtonName { get; }
		public abstract string FunctionName { get; }
		public abstract string[] FunctionNames { get; }
		public abstract Texture2D AtlasTexture { get; }

		public abstract int Width { get; }
		public abstract int Height { get; }

		public override void Start() {
			int bgMouseStates = 0;
			int bgFunctionStates = 0;
			int fgMouseStates = 0;
			int fgFunctionStates = 0;

			bool supportsDisabledBg = false;
			bool supportsDisabledFg = false;

			foreach (ButtonMouseStates mouseState in EnumUtil.GetValues<ButtonMouseStates>()) {
				if (mouseState == ButtonMouseStates.None) {
					continue;
				}

				if ((mouseState & SupportedBgMouseStatesMask) != ButtonMouseStates.None) {
					++bgMouseStates;
				} else if (mouseState == ButtonMouseStates.Base) {
					throw new Exception("Background must support base mouse state");
				}

				if ((mouseState & SupportedFgMouseStatesMask) != ButtonMouseStates.None) {
					++fgMouseStates;
				} else if (mouseState == ButtonMouseStates.Base) {
					throw new Exception("Foreground must support base mouse state");
				}
			}

			foreach (ButtonFunctionStates functionState in EnumUtil.GetValues<ButtonFunctionStates>()) {
				if (functionState == ButtonFunctionStates.None) {
					continue;
				}

				if ((functionState & SupportedBgFunctionStatesMask) != ButtonFunctionStates.None) {
					if (functionState == ButtonFunctionStates.Disabled) {
						supportsDisabledBg = true;
					} else {
						++bgFunctionStates;
					}
				} else if (functionState == ButtonFunctionStates.Default) {
					throw new Exception("Background must support default function state");
				}

				if ((functionState & SupportedFgFunctionStatesMask) != ButtonFunctionStates.None) {
					if (functionState == ButtonFunctionStates.Disabled) {
						supportsDisabledFg = true;
					} else {
						++fgFunctionStates;
					}
				} else if (functionState == ButtonFunctionStates.Default) {
					throw new Exception("Foreground must support default function state");
				}
			}

			string[] textureIds = new string[bgMouseStates * bgFunctionStates + (supportsDisabledBg ? 1 : 0) + FunctionNames.Length * (fgMouseStates * fgFunctionStates + (supportsDisabledFg ? 1 : 0))];

			int i = 0;

			/*
			 * Background textures (Mouse state, Function state):
			 *		(Base, Default)
			 *		(Base, Active)
			 *		(Base, Disabled)
			 *		(Hovered, Default)
			 *		(Hovered, Active)
			 *		(Pressed, Default)
			 *		(Pressed, Active)
			 *		(Focused, Default),
			 *		(Focused, Active)
			 */
			foreach (ButtonMouseStates mouseState in EnumUtil.GetValues<ButtonMouseStates>()) {
				if ((mouseState & SupportedBgMouseStatesMask) == ButtonMouseStates.None) {
					continue;
				}

				foreach (ButtonFunctionStates functionState in EnumUtil.GetValues<ButtonFunctionStates>()) {
					if (functionState == ButtonFunctionStates.Disabled && mouseState != ButtonMouseStates.Base) {
						continue;
					}

					if ((functionState & SupportedBgFunctionStatesMask) == ButtonFunctionStates.None) {
						continue;
					}

					textureIds[i++] = GetButtonBackgroundTextureId(ButtonName, mouseState, functionState);
				}
			}

			/*
			 * Foreground textures (Mouse state, Function state):
			 *		(Base, Default)
			 *		(Base, Active)
			 *		(Base, Disabled)
			 *		(Hovered, Default)
			 *		(Hovered, Active)
			 *		(Pressed, Default)
			 *		(Pressed, Active)
			 *		(Focused, Default),
			 *		(Focused, Active)
			 */
			foreach (string function in FunctionNames) {
				foreach (ButtonMouseStates mouseState in EnumUtil.GetValues<ButtonMouseStates>()) {
					if ((mouseState & SupportedFgMouseStatesMask) == ButtonMouseStates.None) {
						continue;
					}

					foreach (ButtonFunctionStates functionState in EnumUtil.GetValues<ButtonFunctionStates>()) {
						if ((functionState & SupportedFgFunctionStatesMask) == ButtonFunctionStates.None) {
							continue;
						}

						if (functionState == ButtonFunctionStates.Disabled && mouseState != ButtonMouseStates.Base) {
							continue;
						}

						textureIds[i++] = GetButtonForegroundTextureId(ButtonName, function, mouseState, functionState);
					}
				}
			}

			// Set the atlases for background/foreground
			atlas = TextureUtil.GenerateLinearAtlas("TMPE_" + ButtonName + "Atlas", AtlasTexture, textureIds.Length, textureIds);

			m_ForegroundSpriteMode = UIForegroundSpriteMode.Scale;
			UpdateProperties();

			// Enable button sounds.
			playAudioEvents = true;
			foregroundSpriteMode = UIForegroundSpriteMode.Scale;
		}

		public abstract ButtonFunctionStates FunctionState { get; }
		public abstract string Tooltip { get; }
		public abstract bool Visible { get; }
		public abstract void HandleClick(UIMouseEventParameter p);

		protected override void OnClick(UIMouseEventParameter p) {
			HandleClick(p);
			UpdateProperties();
		}

		public void UpdateProperties() {
			//Log.Warning($"{this.GetType().Name}.UpdateProperties called. isVisible={isVisible} enabled={enabled}");

			ButtonFunctionStates bgFuncState = ButtonFunctionStates.Default;
			ButtonFunctionStates fgFuncState = ButtonFunctionStates.Default;

			if ((SupportedBgFunctionStatesMask & FunctionState) != ButtonFunctionStates.None) {
				bgFuncState = FunctionState;
			}

			if ((SupportedFgFunctionStatesMask & FunctionState) != ButtonFunctionStates.None) {
				fgFuncState = FunctionState;
			}

			ButtonMouseStates bgBaseState = ButtonMouseStates.Base;
			m_BackgroundSprites.m_Normal = m_BackgroundSprites.m_Disabled = GetButtonBackgroundTextureId(ButtonName, bgBaseState, bgFuncState);

			ButtonMouseStates bgHoveredState = (SupportedBgMouseStatesMask & ButtonMouseStates.Hovered) != ButtonMouseStates.None ? ButtonMouseStates.Hovered : bgBaseState;
			m_BackgroundSprites.m_Hovered = GetButtonBackgroundTextureId(ButtonName, bgHoveredState, bgFuncState);

			ButtonMouseStates bgPressedState = (SupportedBgMouseStatesMask & ButtonMouseStates.Pressed) != ButtonMouseStates.None ? ButtonMouseStates.Pressed : bgHoveredState;
			m_PressedBgSprite = GetButtonBackgroundTextureId(ButtonName, bgPressedState, bgFuncState);

			ButtonMouseStates bgFocusedState = (SupportedBgMouseStatesMask & ButtonMouseStates.Focused) != ButtonMouseStates.None ? ButtonMouseStates.Focused : bgPressedState;
			m_BackgroundSprites.m_Focused = GetButtonBackgroundTextureId(ButtonName, bgFocusedState, bgFuncState);


			ButtonMouseStates fgBaseState = ButtonMouseStates.Base;
			m_ForegroundSprites.m_Normal = m_ForegroundSprites.m_Disabled = GetButtonForegroundTextureId(ButtonName, FunctionName, fgBaseState, fgFuncState);

			ButtonMouseStates fgHoveredState = (SupportedFgMouseStatesMask & ButtonMouseStates.Hovered) != ButtonMouseStates.None ? ButtonMouseStates.Hovered : fgBaseState;
			m_ForegroundSprites.m_Hovered = GetButtonForegroundTextureId(ButtonName, FunctionName, fgHoveredState, fgFuncState);

			ButtonMouseStates fgPressedState = (SupportedFgMouseStatesMask & ButtonMouseStates.Pressed) != ButtonMouseStates.None ? ButtonMouseStates.Pressed : fgHoveredState;
			m_PressedFgSprite = GetButtonForegroundTextureId(ButtonName, FunctionName, fgPressedState, fgFuncState);

			ButtonMouseStates fgFocusedState = (SupportedFgMouseStatesMask & ButtonMouseStates.Focused) != ButtonMouseStates.None ? ButtonMouseStates.Focused : fgPressedState;
			m_ForegroundSprites.m_Focused = GetButtonForegroundTextureId(ButtonName, FunctionName, fgFocusedState, fgFuncState);

			tooltip = Translation.GetString(Tooltip);
			isVisible = Visible;
			state = FunctionState != ButtonFunctionStates.Disabled ? ButtonState.Normal : ButtonState.Disabled;
			this.Invalidate();

			//Log.Warning($"{this.GetType().Name}.UpdateProperties finished. Visible={Visible} isVisible={isVisible} FunctionState={FunctionState} enabled={enabled}");
		}
	}
}
