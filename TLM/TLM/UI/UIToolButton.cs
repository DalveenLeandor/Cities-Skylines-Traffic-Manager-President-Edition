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
	public abstract class UIToolButton : LinearSpriteButton {
		public enum ButtonFunction {
			Add,
			Cancel,
			Copy,
			Delete,
			Down,
			Edit,
			Load,
			NotOk,
			Ok,
			Remove,
			RotateLeft,
			RotateRight,
			Save,
			Setup,
			Up,
			View
		}

		public const string MENU_BUTTON = "TMPE_ToolButton";
		public const int BUTTON_SIZE = 20;
		public override void HandleClick(UIMouseEventParameter p) { }
		public abstract ButtonFunction Function { get; }
		private ButtonFunctionStates lastFunctionState = ButtonFunctionStates.Default;

		public override string ButtonName {
			get {
				return MENU_BUTTON;
			}
		}

		public override string FunctionName {
			get {
				return Function.ToString();
			}
		}

		public override string[] FunctionNames {
			get {
				var functions = Enum.GetValues(typeof(ButtonFunction));
				string[] ret = new string[functions.Length];
				for (int i = 0; i < functions.Length; ++i) {
					ret[i] = functions.GetValue(i).ToString();
				}
				return ret;
			}
		}

		public override Texture2D AtlasTexture {
			get {
				return TextureResources.ToolButtonsTexture2D;
			}
		}

		public override int Width {
			get {
				return BUTTON_SIZE;
			}
		}

		public override int Height {
			get {
				return BUTTON_SIZE;
			}
		}

		public override ButtonMouseStates SupportedBgMouseStatesMask {
			get {
				return ButtonMouseStates.Base | ButtonMouseStates.Hovered | ButtonMouseStates.Pressed | ButtonMouseStates.Focused;
			}
		}

		public override ButtonFunctionStates SupportedBgFunctionStatesMask {
			get {
				return ButtonFunctionStates.Default | ButtonFunctionStates.Disabled;
			}
		}

		public override ButtonMouseStates SupportedFgMouseStatesMask {
			get {
				return ButtonMouseStates.Base;
			}
		}

		public override ButtonFunctionStates SupportedFgFunctionStatesMask {
			get {
				return ButtonFunctionStates.Default | ButtonFunctionStates.Disabled;
			}
		}

		public override void Update() {
			base.Update();

			if (FunctionState != lastFunctionState || Visible != isVisible) {
				lastFunctionState = FunctionState;
				UpdateProperties();
			}
		}
	}
}
