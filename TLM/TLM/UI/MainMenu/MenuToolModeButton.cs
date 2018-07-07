using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.UI;
using TrafficManager.Manager;

namespace TrafficManager.UI.MainMenu {
	public abstract class MenuToolModeButton : MenuButton {
		public abstract ToolMode ToolMode { get; }

		public override ButtonFunctionStates FunctionState {
			get {
				return this.ToolMode.Equals(UIBase.GetTrafficManagerTool(false)?.GetToolMode()) ? ButtonFunctionStates.Active : ButtonFunctionStates.Default;
			}
		}

		public override void OnClickInternal(UIMouseEventParameter p) {
			if (FunctionState == ButtonFunctionStates.Active) {
				UIBase.GetTrafficManagerTool(true).SetToolMode(ToolMode.None);
			} else {
				UIBase.GetTrafficManagerTool(true).SetToolMode(this.ToolMode);
			}
		}
	}
}
