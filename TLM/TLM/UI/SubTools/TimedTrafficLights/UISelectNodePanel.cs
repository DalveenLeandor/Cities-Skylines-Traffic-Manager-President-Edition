using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TrafficManager.UI.SubTools.TimedTrafficLights {
	public class UISelectNodePanel : UITimedTrafficLightsContentPanel {
		private UILabel selectNodesLabel;
		private UIButton clearSelectionButton;
		private UIButton setupTimedTrafficLightButton;

		public override void Start() {
			base.Start();

			selectNodesLabel = AddUIComponent<UILabel>();
			selectNodesLabel.text = Translation.GetString("Select_nodes") + ": 0";
			selectNodesLabel.relativePosition = new Vector2(0, 0);

			clearSelectionButton = AddUIComponent<UIDeselectNodesButton>();
			clearSelectionButton.relativePosition = new Vector2(width - 90f, 0f);
			clearSelectionButton.anchor = UIAnchorStyle.Right | UIAnchorStyle.Top;

			setupTimedTrafficLightButton = AddUIComponent<UISetupTimedTrafficLightsButton>();
			setupTimedTrafficLightButton.relativePosition = new Vector2(width - 40f, 0f);
			setupTimedTrafficLightButton.anchor = UIAnchorStyle.Right | UIAnchorStyle.Top;
		}

		public override void Update() {
			base.Update();

			selectNodesLabel.text = Translation.GetString("Select_nodes") + ": " + Tool.SelectedNodeIds.Count;
		}

		public override void InitializeContent() {
			selectNodesLabel.text = Translation.GetString("Select_nodes") + ": 0";

			Window.width = 300;
			Window.height = 100;
		}

		public class UIDeselectNodesButton : UIToolButton {
			public override ButtonFunction Function {
				get {
					return ButtonFunction.NotOk;
				}
			}

			public override ButtonFunctionStates FunctionState {
				get {
					return UISelectNodePanel.Instance.Tool.SelectedNodeIds.Count > 0 ? ButtonFunctionStates.Default : ButtonFunctionStates.Disabled;
				}
			}

			public override int Width {
				get {
					return 40;
				}
			}

			public override int Height {
				get {
					return 40;
				}
			}

			public override string Tooltip {
				get {
					return Translation.GetString("Deselect_all_nodes");
				}
			}

			public override bool Visible {
				get {
					return true;
				}
			}

			public override void HandleClick(UIMouseEventParameter p) {
				base.HandleClick(p);

				UISelectNodePanel.Instance.Tool.ClearSelectedNodes();
			}
		}

		public class UISetupTimedTrafficLightsButton : UIToolButton {
			public override ButtonFunction Function {
				get {
					return ButtonFunction.Setup;
				}
			}

			public override ButtonFunctionStates FunctionState {
				get {
					return UISelectNodePanel.Instance.Tool.SelectedNodeIds.Count > 0 ? ButtonFunctionStates.Default : ButtonFunctionStates.Disabled;
				}
			}

			public override int Width {
				get {
					return 40;
				}
			}

			public override int Height {
				get {
					return 40;
				}
			}

			public override string Tooltip {
				get {
					return Translation.GetString("Setup_timed_traffic_light");
				}
			}

			public override bool Visible {
				get {
					return true;
				}
			}

			public override void HandleClick(UIMouseEventParameter p) {
				base.HandleClick(p);

				UISelectNodePanel.Instance.Tool.SetupTimedTrafficLights();
			}
		}
	}
}
