using ColossalFramework.UI;
using CSUtil.Commons;
using CSUtil.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static TrafficManager.UI.SubTools.TimedTrafficLightsTool;

namespace TrafficManager.UI.SubTools.TimedTrafficLights {
	public class UITimedTrafficLightsWindow : UISingletonWindow<UITimedTrafficLightsWindow> {
		private IDictionary<TimedTrafficLightsToolMode, UITimedTrafficLightsContentPanel> contentPanels;

		public TimedTrafficLightsTool Tool {
			get {
				return UIBase.GetTrafficManagerTool().GetSubTool(ToolMode.TimedLights) as TimedTrafficLightsTool; // TODO remove subtool modes
			}
		}
		private TimedTrafficLightsToolMode mode;
		
		public override void Start() {
			Log._Debug("UITimedTrafficLightsWindow.Start [START]");

			Closable = false;
			Draggable = true;
			Title = Translation.GetString("Timed_traffic_lights_manager");

			Rect dimensions = /*TrafficManagerTool.MoveGUI(*/new Rect(0, 0, 100, 100)/*)*/;
			relativePosition = dimensions.position;
			size = dimensions.size;

			// create panels
			contentPanels = new Dictionary<TimedTrafficLightsToolMode, UITimedTrafficLightsContentPanel>();
			contentPanels.Add(TimedTrafficLightsToolMode.SelectNode, AddContentPanel<UISelectNodePanel>());
			UIShowLightsPanel showLightsPanel = AddContentPanel<UIShowLightsPanel>();
			contentPanels.Add(TimedTrafficLightsToolMode.ShowLights, showLightsPanel);
			contentPanels.Add(TimedTrafficLightsToolMode.ViewStep, showLightsPanel);
			UIModifyStepPanel modifyStepPanel = AddContentPanel<UIModifyStepPanel>();
			contentPanels.Add(TimedTrafficLightsToolMode.AddStep, modifyStepPanel);
			contentPanels.Add(TimedTrafficLightsToolMode.EditStep, modifyStepPanel);
			contentPanels.Add(TimedTrafficLightsToolMode.AddNode, AddContentPanel<UIAddNodePanel>());
			contentPanels.Add(TimedTrafficLightsToolMode.RemoveNode, AddContentPanel<UIRemoveNodePanel>());
			contentPanels.Add(TimedTrafficLightsToolMode.Copy, AddContentPanel<UICopyPanel>());

			mode = TimedTrafficLightsToolMode.None;

			base.Start();

			Log._Debug("UITimedTrafficLightsWindow.Start [END]");
		}

		public void Cleanup() {
			mode = TimedTrafficLightsToolMode.None;
		}

		protected override T AddContentPanel<T>() {
			T ret = base.AddContentPanel<T>();
			ret.isEnabled = ret.isVisible = false;
			return ret;
		}

		public void UpdateBindings() {
			TimedTrafficLightsToolMode newMode = Tool.Mode;
			Log._Debug($"UITimedTrafficLightsWindow.UpdateBindings [START] newMode={newMode}");

			bool init = newMode != this.mode;
			this.mode = newMode;

			if (contentPanels == null) {
				Log.Error($"contentPanels is null!");
			}
			if (contentPanels != null) {
				UITimedTrafficLightsContentPanel shownContentPanel = null;
				foreach (KeyValuePair<TimedTrafficLightsToolMode, UITimedTrafficLightsContentPanel> e in contentPanels) {
					if (e.Key == mode) {
						e.Value.isEnabled = e.Value.isVisible = true;
						if (init) {
							e.Value.InitializeContent();
						}
						shownContentPanel = e.Value;
					} else {
						if (shownContentPanel == e.Value) {
							// some content panels are used in multiple modes
							continue;
						}

						e.Value.isEnabled = e.Value.isVisible = false;
					}
				}
			}

			if (mode == TimedTrafficLightsToolMode.None) {
				Hide();
			} else {
				Show();
			}

			Log._Debug("UITimedTrafficLightsWindow.UpdateBindings [END]");
		}
	}
}
