using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSUtil.UI {
	public class UISingletonWindowContentPanel<T> : UIWindowContentPanel where T : UISingletonWindowContentPanel<T> {
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
