using ColossalFramework.UI;
using CSUtil.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CSUtil.UI {
	public abstract class UIWindow : UIPanel {
		protected const float CONTENT_BORDER = 10f;
		protected const float RIGHT_CAPTION_BORDER = 36f;
		protected const float CAPTION_HEIGHT = 40f;

		private UIDragHandle dragHandle;
		private UIButton closeButton;
		private UILabel captionLabel;
		protected UIDynamicFont HeaderFont { get; private set; }

		public bool Draggable { get; protected set; } = true;
		public bool Closable { get; protected set; } = true;
		public string Title { get; protected set; } = "";

		public virtual string GetCachedName() {
			return GetType().Name;
		}

		public override void Start() {
			base.Start();
			HeaderFont = Resources.FindObjectsOfTypeAll<UIDynamicFont>().Where(f => string.Equals("OpenSans-Regular", f.name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

			backgroundSprite = "MenuPanel2";
			canFocus = true;
			isInteractive = true;
			opacity = 1f;
			cachedName = GetCachedName();

			// add close button
			closeButton = AddUIComponent<UIButton>();
			closeButton.normalBgSprite = "buttonclose";
			closeButton.hoveredBgSprite = "buttonclosehover";
			closeButton.pressedBgSprite = "buttonclosepressed";
			closeButton.relativePosition = new Vector2(this.size.x - RIGHT_CAPTION_BORDER, 3f);
			closeButton.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right;
			closeButton.eventClick += (component, eventParam) => {
				Hide();
			};

			// add window caption
			captionLabel = AddUIComponent<UILabel>();
			captionLabel.autoSize = false;
			UIDynamicFont font = UIDynamicFont.FindByName("OpenSans-Regular");
			captionLabel.font = HeaderFont;
			captionLabel.text = Title;
			captionLabel.relativePosition = new Vector2(0, (CAPTION_HEIGHT - captionLabel.height) / 2f);
			captionLabel.width = width - (Closable ? RIGHT_CAPTION_BORDER : 0);
			//captionLabel.relativePosition = new Vector3((size.x - captionLabel.width) / 2f, (CAPTION_HEIGHT - captionLabel.height) / 2f);
			captionLabel.textAlignment = UIHorizontalAlignment.Center;
			captionLabel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right;

			// add drag handle
			dragHandle = AddUIComponent<UIDragHandle>();
			dragHandle.target = this;
			dragHandle.relativePosition = new Vector2(0, 0);
			dragHandle.size = new Vector2(size.x - (Closable ? RIGHT_CAPTION_BORDER : 0), CAPTION_HEIGHT);
			dragHandle.enabled = true;
			dragHandle.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right;

			Hide();
		}

		public override void Update() {
			base.Update();

			bool closableChanged = closeButton.isVisible != Closable;

			dragHandle.enabled = Draggable;
			closeButton.isVisible = Closable;

			if (closableChanged) {
				captionLabel.width = width - (Closable ? RIGHT_CAPTION_BORDER : 0);
				dragHandle.size = new Vector2(size.x - (Closable ? RIGHT_CAPTION_BORDER : 0), CAPTION_HEIGHT);
			}

			if (Closable && Input.GetKey(KeyCode.Escape)) {
				Hide();
			}
		}

		protected virtual T AddContentPanel<T>() where T : UIPanel {
			T panel = AddUIComponent<T>();
			panel.relativePosition = new Vector2(CONTENT_BORDER, CAPTION_HEIGHT + CONTENT_BORDER);
			panel.size = new Vector2(size.x - 2f * CONTENT_BORDER, size.y - CAPTION_HEIGHT - 2f * CONTENT_BORDER);
			panel.anchor = UIAnchorStyle.All;
			return panel;
		}
	}
}
