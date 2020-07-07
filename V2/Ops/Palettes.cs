using System;
using System.Collections.Generic;
using System.Text;
using NESSharp.Common;
using NESSharp.Core;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class Palettes {
		private U8 _opPalettes;
		private LiveQueue _liveQueue;
		private Label _executeLoopContinue;
		public Palettes(Func<Label, U8> handlerListAdd, LiveQueue queue, Label execContinue, Label _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opPalettes = handlerListAdd(LabelFor(Handler));
		}
		public void Write(Core.Address addr) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opPalettes);
				_liveQueue.Push(addr.Lo);
				_liveQueue.Push(addr.Hi);
			});
		}
		public void Write(Label lbl) {
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opPalettes);
				_liveQueue.Push(lbl.Lo());
				_liveQueue.Push(lbl.Hi());
			});
		}
		public void Write(Action dataSection) {
			var lbl = LabelFor(dataSection);
			_liveQueue.Write(Y, () => {
				_liveQueue.Push(_opPalettes);
				_liveQueue.Push(lbl.Lo());
				_liveQueue.Push(lbl.Hi());
			});
		}
		[CodeSection]
		private void Handler() {
			_liveQueue.Unsafe_Pop(Y);
			TempPtr0.Lo.Set(_liveQueue.Unsafe_Peek(Y));
			_liveQueue.Unsafe_Pop(Y);
			TempPtr0.Hi.Set(_liveQueue.Unsafe_Peek(Y));

			Stack.Preserve(Y, () => {
				Y.Set(0);
				NES.PPU.SetAddress(NES.MemoryMap.Palette);
				Loop.AscendWhile(Y.Set(0), () => Y.NotEquals(32), () => {
					NES.PPU.Data.Set(TempPtr0[Y]);
				});
			});

			GoTo(_executeLoopContinue);
		}
	}
}
