using NESSharp.Common;
using NESSharp.Core;
using System;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.VRamQueue.V2.Ops {
	public class Increment : VRamQueueOp {
		private U8 _opHoriz;
		private U8 _opVert;
		private LiveQueue _liveQueue;
		public override void AddHandlers() => Queue.Add(Handler_Horiz);
		public Increment(Func<Label, U8> handlerListAdd, LiveQueue queue) {
			//TODO: add a way to store multiple op IDs, one per callback
			_liveQueue = queue;
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
			Queue.Continue();
		}
		[CodeSection]
		private void Handler_Vert() {
			Comment("Increment writes vertically");
			NES.PPU.Control.Set(NES.PPU.LazyControl.Set(z => z.Or(0b100)));
			Queue.Continue();
		}
	}
}
