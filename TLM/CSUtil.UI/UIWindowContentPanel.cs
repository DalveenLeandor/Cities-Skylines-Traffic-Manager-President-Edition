using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CSUtil.UI {
	public abstract class UIWindowContentPanel : UIPanel {
		protected virtual UIButton AddButton() {
			UIButton button = AddUIComponent<UIButton>();
			button.normalBgSprite = "ButtonMenu";
			button.disabledBgSprite = "ButtonMenuDisabled";
			button.hoveredBgSprite = "ButtonMenuHovered";
			button.focusedBgSprite = "ButtonMenu";
			button.pressedBgSprite = "ButtonMenuPressed";
			button.textColor = new Color32(255, 255, 255, 255);
			button.textPadding = new RectOffset(5, 5, 5, 5);
			button.playAudioEvents = true;
			return button;
		}
	}
}
