using ColossalFramework.UI;
using CSUtil.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CSUtil.UI {
	public abstract class UICellRow : UIPanel {
		public enum UICellWidthMode {
			Auto,
			Relative,
			Absolute,
		}

		public abstract float Width { get; }
		public abstract float Height { get; }
		public abstract float Margin { get; }
		public abstract bool Odd { get; }

		public static readonly Color32 ODD_COLOR = new Color32(46, 50, 56, 255);
		public static readonly Color32 EVEN_COLOR = new Color32(53, 59, 61, 255);

		public abstract IList<UICellDefinition> CellDefinitions { get; }

		public override void Start() {
			base.Start();

			foreach (UICellDefinition cellDef in CellDefinitions) {
				UIPanel comp = AddUIComponent<UIPanel>();
				comp.autoSize = false;
			}

			width = Width;
			height = Height;
			backgroundSprite = "InfoviewPanel";
			color = new Color32(49, 52, 58, 255);
		}

		public override void Update() {
			base.Update();

			if (CellDefinitions.Count <= 0) {
				return;
			}

			// set color based on oddness
			color = Odd ? ODD_COLOR : EVEN_COLOR;

			float totalWidth = width - ((float)(CellDefinitions.Count - 1) * Margin);
			//Log._Debug($"UICellRow.Update(): totalWidth={totalWidth} width={width} CellDefinitions.Count={CellDefinitions.Count} Margin={Margin}");

			// accumulate total reserved pixels, count auto-width cells
			float absReservedPixels = 0f;
			int numAutoCells = 0;
			foreach (UICellDefinition cellDef in CellDefinitions) {
				switch (cellDef.WidthMode) {
					case UICellWidthMode.Auto:
						++numAutoCells;
						break;
					case UICellWidthMode.Absolute:
						absReservedPixels += cellDef.WidthValue;
						break;
					case UICellWidthMode.Relative:
						absReservedPixels += (cellDef.WidthValue / 100f) * totalWidth;
						break;
				}
			}

			//Log._Debug($"UICellRow.Update(): absReservedPixels={absReservedPixels} numAutoCells={numAutoCells}");

			// calculate correction factor in case total required width exceeds actual width
			float correctionFactor = 1f;
			float autoCellWidth = 0f;
			if (absReservedPixels > totalWidth) {
				correctionFactor = totalWidth / absReservedPixels;
			} else if (numAutoCells > 0) {
				// distribute remaining width evenly to auto-width cells
				float fixedWidth = correctionFactor * absReservedPixels;
				float remainingWidth = totalWidth - fixedWidth;
				autoCellWidth = remainingWidth / (float)numAutoCells;
			}

			//Log._Debug($"UICellRow.Update(): correctionFactor={correctionFactor} autoCellWidth={autoCellWidth}");

			// apply widths and calculate positions
			float x = 0f;
			for (int i = 0; i < CellDefinitions.Count; ++i) {
				UIComponent comp = components[i];
				UICellDefinition cellDef = CellDefinitions[i];

				switch (cellDef.WidthMode) {
					case UICellWidthMode.Auto:
						comp.width = autoCellWidth;
						break;
					case UICellWidthMode.Absolute:
						comp.width = cellDef.WidthValue * correctionFactor;
						break;
					case UICellWidthMode.Relative:
						comp.width = (cellDef.WidthValue / 100f) * totalWidth;
						break;
				}
				comp.height = Height;
				comp.relativePosition = new Vector2(x, 0);

				//Log._Debug($"UICellRow.Update(): components[{i}]: cellDef.WidthMode={cellDef.WidthMode} cellDef.WidthValue={cellDef.WidthValue} relativePosition={relativePosition} comp.width={comp.width} comp.height={comp.height}");
				x += comp.width + Margin;
			}
		}

		public class UICellDefinition {
			public UICellWidthMode WidthMode { get; private set; }
			public float WidthValue { get; private set; }

			public UICellDefinition() {
				this.WidthMode = UICellWidthMode.Auto;
				this.WidthValue = 0f;
			}

			public UICellDefinition(UICellWidthMode widthMode, float widthValue) {
				this.WidthMode = widthMode;
				this.WidthValue = widthValue;
			}
		}
	}
}
