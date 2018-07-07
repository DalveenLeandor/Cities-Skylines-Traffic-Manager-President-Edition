using ColossalFramework.UI;
using CSUtil.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static TrafficManager.UI.SubTools.TimedTrafficLightsTool;

namespace TrafficManager.UI.SubTools.TimedTrafficLights {
	public abstract class UITimedTrafficLightsContentPanel : UISingletonWindowContentPanel<UITimedTrafficLightsContentPanel> {
		public UITimedTrafficLightsWindow Window {
			get {
				return UITimedTrafficLightsWindow.Instance;
			}
		}

		public TimedTrafficLightsTool Tool {
			get {
				return Window.Tool;
			}
		}

		public override void Start() {
			base.Start();
		}

		public abstract void InitializeContent();
	}
}
