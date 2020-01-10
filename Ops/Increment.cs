using NESSharp.Common;
using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.Ops {
	public class Increment {
		private U8 _opHoriz;
		private U8 _opVert;
		private LiveQueue _liveQueue;
		private OpLabel _executeLoopContinue;
		public Increment(Func<OpLabel, U8> handlerListAdd, LiveQueue queue, OpLabel execContinue, OpLabel _) {
			_liveQueue = queue;
			_executeLoopContinue = execContinue;
			_opHoriz = handlerListAdd(LabelFor(Handler_Horiz));
			_opVert = handlerListAdd(LabelFor(Handler_Vert));
		}
		public void Horizontal() {
			_liveQueue.PushOnce(Y, _opHoriz);
		}
		public void Vertical() {
			_liveQueue.PushOnce(Y, _opVert);
		}
		[CodeSection]
		private void Handler_Horiz() {
			Comment("Increment writes horizontally");
			NES.PPU.Control.Set(NES.PPU.LazyControl.Set(z => z.And(0b11111011)));
			GoTo(_executeLoopContinue);
		}
		[CodeSection]
		private void Handler_Vert() {
			Comment("Increment writes vertically");
			NES.PPU.Control.Set(NES.PPU.LazyControl.Set(z => z.Or(0b100)));
			GoTo(_executeLoopContinue);
		}
	}
}
