using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSUtil.UI {
	public class UISingletonWindow<T> : UIWindow where T : UISingletonWindow<T> {
		public static T Instance { get; private set; }

		public override void Start() {
			Instance = (T)this;
			base.Start();
		}

		public override void OnDestroy() {
			Instance = null;
			base.OnDestroy();
		}
	}
}
