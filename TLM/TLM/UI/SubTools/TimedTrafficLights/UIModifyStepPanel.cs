using ColossalFramework.UI;
using CSUtil.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TrafficManager.UI.SubTools.TimedTrafficLights {
	public class UIModifyStepPanel : UITimedTrafficLightsContentPanel {
		UILabel test;

		public override void Start() {
			base.Start();
			
			test = AddUIComponent<UILabel>();
			test.text = this.GetType().Name;
			test.relativePosition = new Vector2(0, 0);
		}

		public override void InitializeContent() {
			
		}
	}
}
