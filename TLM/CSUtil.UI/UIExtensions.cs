using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSUtil.UI {
	public static class UIExtensions {
		public static void Destroy(this UIComponent component) {
			foreach (UIComponent child in component.components) {
				child.Destroy();
			}
			UnityEngine.Object.Destroy(component);
		}
	}
}
